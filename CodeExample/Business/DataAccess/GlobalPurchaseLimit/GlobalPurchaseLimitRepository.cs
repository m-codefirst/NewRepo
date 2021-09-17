using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using TRM.Web.Constants;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.GlobalPurchaseLimits;

namespace TRM.Web.Business.DataAccess.GlobalPurchaseLimit
{
    public class GlobalPurchaseLimitResult
    {
        public GlobalPurchaseLimitResult()
        {
            Messages = new List<string>();
        }

        public List<string> Messages { get; set; }
    }

    [ServiceConfiguration(typeof(IGlobalPurchaseLimitRepository), Lifecycle = ServiceInstanceScope.Singleton)]
    public class GlobalPurchaseLimitRepository : DbContextDisposable<GlobalPurchaseLimitsDbContext>, IGlobalPurchaseLimitRepository
    {
        private static object lockObj = new object();

        public Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit AddOrUpdate(Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit globalPurchaseLimit)
        {
            var existing = context.GlobalPurchaseLimits
                .FirstOrDefault(x => x.Metal == globalPurchaseLimit.Metal
                && x.Hour == globalPurchaseLimit.Hour);

            if (existing != null)
            {
                existing.TotalQuantityBought = globalPurchaseLimit.TotalQuantityBought;
                existing.TotalQuantitySold = globalPurchaseLimit.TotalQuantitySold;
                existing.TotalSignatureBought = globalPurchaseLimit.TotalSignatureBought;
                existing.TotalSignatureSold = globalPurchaseLimit.TotalSignatureSold;
                existing.Day = globalPurchaseLimit.Day;
                globalPurchaseLimit = existing;
            }
            else
            {
                context.GlobalPurchaseLimits.Add(globalPurchaseLimit);
            }

            context.SaveChanges();
            return globalPurchaseLimit;
        }

        public IEnumerable<Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit> Filter(Func<Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit, bool> where)
        {
            return context.GlobalPurchaseLimits.Where(where).ToList();
        }

        /// <summary>
        /// Check the metal limit has been exceeded
        /// </summary>
        public bool PurchaseLimitExceeded(GlobalPurchaseLimitSetting pampMetal, out decimal remainingAmount)
        {
            return IsPurchaseLimitExceeded(pampMetal, out remainingAmount);
        }

        /// <summary>
        /// Check the metal limit has been exceeded
        /// </summary>
        public bool SignaturePurchaseLimitExceeded(GlobalPurchaseLimitSetting pampMetal, out decimal remainingAmount)
        {

            return IsSignaturePurchaseLimitExceeded(pampMetal, out remainingAmount);
        }

        /// <summary>
        /// Update the limitation of Metal based on Global Purchase Limit settings.
        /// </summary>
        /// <param name="metal">use Code of PampMetal</param>
        /// <param name="amount">Amount in TroyOz</param>
        /// <param name="bullionTradeType"><seealso cref="TRM.Web.Constants.Enums.BullionTradeType"/></param>
        /// <param name="checkDate">The date to compare for update the limit.</param>
        /// <returns>true if the limitation exceeded.</returns>
        public GlobalPurchaseLimitResult UpdateMetalPurchaseLimit(string metal, decimal amount, Enums.BullionTradeType bullionTradeType, DateTime checkDate)
        {
            var result = new GlobalPurchaseLimitResult
            {
                Messages = new List<string>(),
            };

            if (checkDate == DateTime.MinValue) checkDate = DateTime.UtcNow;
            lock (lockObj)
            {
                var matchedRow = context.GlobalPurchaseLimits.FirstOrDefault(x => x.Metal == metal && x.Hour == checkDate.Hour && x.Day == checkDate.Day);

                if (matchedRow == null)
                {
                    matchedRow = new Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit
                    {
                        Day = checkDate.Day,
                        Hour = checkDate.Hour,
                        Metal = metal
                    };
                }

                switch (bullionTradeType)
                {
                    case Enums.BullionTradeType.PhysicalBuy:
                        matchedRow.TotalQuantityBought += amount;
                        break;
                    case Enums.BullionTradeType.PhysicalSell:
                        matchedRow.TotalQuantitySold += amount;
                        break;
                    case Enums.BullionTradeType.SignatureBuy:
                        matchedRow.TotalSignatureBought += amount;
                        break;
                    case Enums.BullionTradeType.SignatureSell:
                        matchedRow.TotalSignatureSold += amount;
                        break;
                }
                AddOrUpdate(matchedRow);
                return result;
            }
        }

        /// <summary>
        /// Check the limitation of the meta and return remaining amount of the limit.
        /// </summary>
        /// <param name="pampMetalSetting">The code of PampMetal</param>
        /// <param name="remainingAmount">Remaining amount of the metal purchasing limitation</param>
        /// <returns>Return true if the limit has been exceeded.</returns>
        private bool IsPurchaseLimitExceeded(GlobalPurchaseLimitSetting pampMetalSetting, out decimal remainingAmount)
        {
            remainingAmount = decimal.MaxValue;

            if (pampMetalSetting == null) return false;

            var pampMetalLimits = (from gpl in context.GlobalPurchaseLimits
                                   where gpl.Metal == pampMetalSetting.MetalType.ToString()
                                   group gpl by gpl.Metal into limitGroups
                                   select new
                                   {
                                       Metal = limitGroups.Key,
                                       TotalQuantityBought = limitGroups.Sum(x => x.TotalQuantityBought),
                                       TotalQuantitySold = limitGroups.Sum(x => x.TotalQuantitySold),
                                       TotalSignatureBought = limitGroups.Sum(x => x.TotalSignatureBought),
                                       TotalSignatureSold = limitGroups.Sum(x => x.TotalSignatureSold)
                                   }).FirstOrDefault();

            if (pampMetalLimits == null) return false;

            var totalBought = pampMetalLimits.TotalQuantityBought + pampMetalLimits.TotalSignatureBought;
            var totalSold = pampMetalLimits.TotalQuantitySold + pampMetalLimits.TotalSignatureSold;

            var positionLimit = Math.Abs(totalBought - totalSold);
            var volumeLimit = totalBought + totalSold;

            var deltaVolume = pampMetalSetting.Volume - volumeLimit;
            var deltaPosition = pampMetalSetting.Position - positionLimit;

            remainingAmount = deltaPosition < deltaVolume ? deltaPosition : deltaVolume;

            return (pampMetalSetting.Position > 0 && positionLimit > pampMetalSetting.Position) ||
                   (pampMetalSetting.Volume > 0 && volumeLimit > pampMetalSetting.Volume);
        }

        /// <summary>
        /// Check the limitation of the meta and return remaining amount of the limit.
        /// </summary>
        /// <param name="pampMetalSetting">The code of PampMetal</param>
        /// <param name="remainingAmount">Remaining amount of the metal purchasing limitation</param>
        /// <returns>Return true if the limit has been exceeded.</returns>
        private bool IsSignaturePurchaseLimitExceeded(GlobalPurchaseLimitSetting pampMetalSetting, out decimal remainingAmount)
        {
            remainingAmount = decimal.MaxValue;

            if (pampMetalSetting == null) return false;

            var pampMetalLimits = (from gpl in context.GlobalPurchaseLimits
                                   where gpl.Metal == pampMetalSetting.MetalType.ToString()
                                   group gpl by gpl.Metal into limitGroups
                                   select new
                                   {
                                       Metal = limitGroups.Key,
                                       TotalSignatureBought = limitGroups.Sum(x => x.TotalSignatureBought),
                                   }).FirstOrDefault();

            if (null == pampMetalLimits) return false;

            remainingAmount = pampMetalSetting.SignatureVolume - pampMetalLimits.TotalSignatureBought;

            return pampMetalSetting.SignatureVolume > 0 && remainingAmount < 0;
        }
    }
}