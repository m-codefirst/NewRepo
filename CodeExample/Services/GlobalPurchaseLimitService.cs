using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using TRM.Web.Business.DataAccess.GlobalPurchaseLimit;
using TRM.Web.Business.Email;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.Pages;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Services
{
    public interface IGlobalPurchaseLimitService
    {
        GlobalPurchaseLimitResult MonitorPurchaseLimitExceeded();
        GlobalPurchaseLimitResult UpdateMetalPurchaseLimit(string metal, decimal quantityInOz, BullionTradeType bullionTradeType);
        void UpdateGlobalPurchaseLimits(IPurchaseOrder purchaseOrder);
    }

    [ServiceConfiguration(typeof(IGlobalPurchaseLimitService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class GlobalPurchaseLimitService : IGlobalPurchaseLimitService
    {
        protected readonly ILogger Logger = LogManager.GetLogger(typeof(GlobalPurchaseLimitService));

        private readonly IContentRepository _contentRepository;
        private readonly IGlobalPurchaseLimitRepository _globalPurchaseLimitRepository;
        private readonly IEmailHelper _emailHelper;

        public GlobalPurchaseLimitService(
            IContentRepository contentRepository,
            IGlobalPurchaseLimitRepository globalPurchaseLimitRepository,
            IEmailHelper emailHelper)
        {
            _emailHelper = emailHelper;
            _contentRepository = contentRepository;
            _globalPurchaseLimitRepository = globalPurchaseLimitRepository;
        }

        public GlobalPurchaseLimitResult MonitorPurchaseLimitExceeded()
        {
            var result = new GlobalPurchaseLimitResult();

            var startPage = _contentRepository.Get<StartPage>(ContentReference.StartPage);
            if (startPage == null)
            {
                result.Messages.Add("Job was cancelled. Start page not set yet!");
                return result;
            }

            if (startPage.GlobalPurchaseLimitSettings == null || !startPage.GlobalPurchaseLimitSettings.Any())
            {
                result.Messages.Add("Job was cancelled. Global Purchase Limit Settings in Start Page has not set yet!");
                return result;
            }

            var pampMetals = startPage.GlobalPurchaseLimitSettings;

            var needToTriggerStopTrading = false;
            foreach (var metal in pampMetals)
            {
                decimal remaining = 0;

                var exceeded = _globalPurchaseLimitRepository.PurchaseLimitExceeded(metal, out remaining);
                if (exceeded)
                {
                    result.Messages.Add($"The metal: {metal.MetalType.ToString()} has been exceeded.");
                    needToTriggerStopTrading = true;
                }
                else
                {
                    result.Messages.Add($"Remaining amount of {metal.MetalType.ToString()}: {remaining}");
                }

                var signatureExceeded = _globalPurchaseLimitRepository.SignaturePurchaseLimitExceeded(metal, out remaining);
                if (signatureExceeded)
                {
                    result.Messages.Add($"The signature {metal.MetalType.ToString()} has been exceeded.");
                    result.Messages.AddRange(SendSignaturePurchaseLimitEmail(startPage));
                }
            }

            if (needToTriggerStopTrading)
            {
                result.Messages.AddRange(TriggerGlobalStopTrading(startPage));
            }

            return result;
        }

        private IEnumerable<string> TriggerGlobalStopTrading(StartPage startPage)
        {
            var messages = new List<string>();

            messages.AddRange(SendGlobalPurchaseLimitEmail(startPage));

            if (startPage.StopTrading) return messages;

            var editableStartPage = startPage.CreateWritableClone() as StartPage;
            if (editableStartPage != null)
            {
                editableStartPage.StopTrading = true;
                _contentRepository.Publish(editableStartPage, AccessLevel.NoAccess);

                messages.Add("StopTrading set to true.");
            }

            return messages;
        }

        private IEnumerable<string> SendGlobalPurchaseLimitEmail(StartPage startPage)
        {
            var messages = new List<string>();

            if (string.IsNullOrEmpty(startPage.GlobalPurchaseLimitToEmail))
            {
                messages.Add("The  email page for Global Purchase Limits not set from Start page yet!");
            }

            if (startPage.GlobalPurchaseLimitEmailPage == null) return messages;

            var globalPurchaseLimitEmailPage = _contentRepository.Get<TRMEmailPage>(startPage.GlobalPurchaseLimitEmailPage);
            if (globalPurchaseLimitEmailPage == null)
            {
                messages.Add("The email page for Global Purchase Limits not set from Start page yet!");
            }

            _emailHelper.SendGlobalPurchaseLimitEmail(globalPurchaseLimitEmailPage, startPage.GlobalPurchaseLimitToEmail);
            messages.Add($"Sent email to {startPage.GlobalPurchaseLimitToEmail}.");

            return messages;
        }

        private IEnumerable<string> SendSignaturePurchaseLimitEmail(StartPage startPage)
        {
            var messages = new List<string>();

            if (string.IsNullOrEmpty(startPage.GlobalPurchaseLimitToEmail))
            {
                messages.Add("The  email page for Global Purchase Limits not set from Start page yet!");
            }

            if (startPage.SignaturePurchaseLimitEmailPage == null) return messages;

            var globalPurchaseLimitEmailPage = _contentRepository.Get<TRMEmailPage>(startPage.SignaturePurchaseLimitEmailPage);
            if (globalPurchaseLimitEmailPage == null)
            {
                messages.Add("The email page for Signature Purchase Limits not set from Start page yet!");
            }

            _emailHelper.SendGlobalPurchaseLimitEmail(globalPurchaseLimitEmailPage, startPage.GlobalPurchaseLimitToEmail);
            messages.Add($"Sent email to {startPage.GlobalPurchaseLimitToEmail}.");

            return messages;
        }

        public GlobalPurchaseLimitResult UpdateMetalPurchaseLimit(string metalCode, decimal quantityInOz, BullionTradeType bullionTradeType)
        {
            try
            {
                var checkDate = DateTime.UtcNow;

                ResetCounter(metalCode, checkDate);

                var exceeded = _globalPurchaseLimitRepository.UpdateMetalPurchaseLimit(metalCode, quantityInOz, bullionTradeType, checkDate);

                return exceeded;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateMetalPurchaseLimit Error", e);
                return null;
            }
        }

        public void UpdateGlobalPurchaseLimits(IPurchaseOrder purchaseOrder)
        {
            try
            {
                var lineItems = purchaseOrder?.GetAllLineItems();
                if (lineItems == null || !lineItems.Any()) return;

                foreach (var lineItem in lineItems)
                {
                    var metalCode = string.Empty;
                    var bullionTradeType = BullionTradeType.PhysicalBuy;
                    decimal troyOzConfig = 0;
                    var variant = lineItem.GetEntryContent();

                    var virtualVariant = variant as VirtualVariantBase;

                    if (virtualVariant != null)
                    {
                        metalCode = virtualVariant.MetalType.ToString();
                        bullionTradeType = BullionTradeType.SignatureBuy;
                        troyOzConfig = virtualVariant.TroyOzWeightConfiguration;
                    }

                    var physicalVariant = variant as PhysicalVariantBase;
                    if (physicalVariant != null)
                    {
                        metalCode = physicalVariant.MetalType.ToString();
                        bullionTradeType = BullionTradeType.PhysicalBuy;
                        troyOzConfig = physicalVariant.TroyOzWeightConfiguration;
                    }

                    if (string.IsNullOrEmpty(metalCode)) continue;

                    UpdateMetalPurchaseLimit(metalCode, lineItem.Quantity * troyOzConfig, bullionTradeType);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"UpdateGlobalPurchaseLimits: {ex.Message} {ex.InnerException?.Message}", ex);
            }
        }

        private void ResetCounter(string metal, DateTime checkDate)
        {
            var yesterday = checkDate.AddDays(-1);
            var limits = _globalPurchaseLimitRepository.Filter(x => x.Metal == metal);
            foreach (var limit in limits)
            {
                if ((limit.Day != checkDate.Day && limit.Hour <= checkDate.Hour) || (limit.Day != yesterday.Day && limit.Hour > checkDate.Hour))
                {
                    limit.TotalQuantityBought = 0;
                    limit.TotalQuantitySold = 0;
                    limit.TotalSignatureBought = 0;
                    limit.TotalSignatureSold = 0;
                    limit.Day = checkDate.Day;

                    _globalPurchaseLimitRepository.AddOrUpdate(limit);
                }
            }
        }
    }
}