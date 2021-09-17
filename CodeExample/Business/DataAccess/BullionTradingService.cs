using Castle.Core.Internal;
using EPiServer.ServiceLocation;
using log4net;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Newtonsoft.Json;
using PricingAndTradingService.Models;
using PricingAndTradingService.Models.APIRequests;
using PricingAndTradingService.Models.APIResponse;
using PricingAndTradingService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using EPiServer;
using EPiServer.Web;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.PampQuoteRequest;
using TRM.Web.Models.Pages;
using static PricingAndTradingService.Models.Constants;
using static TRM.Shared.Constants.StringConstants;
using DealerTradeSide = PricingAndTradingService.Models.Constants.DealerTradeSide;

namespace TRM.Web.Business.DataAccess
{
    public static class RequestCommand
    {
        public static string PreRequestForQuote = nameof(PreRequestForQuote);
        public static string RequestForQuote = nameof(RequestForQuote);
        public static string FinishQuoteRequest = nameof(FinishQuoteRequest);
    }

    public interface IBullionTradingService
    {
        QuoteResponse RequestPampForQuote(
            IEnumerable<MetalQuantity> metalQuantities,
            DealerTradeSide dealerTradeSide = DealerTradeSide.PampSells,
            Func<IAmPremiumVariant, decimal> getQuantityInOzFunc = null);

        QuoteResponse RequestPampForQuote(
            CustomerContact customerContact,
            IEnumerable<MetalQuantity> metalQuantities,
            DealerTradeSide dealerTradeSide = DealerTradeSide.PampSells,
            Func<IAmPremiumVariant, decimal> getQuantityInOzFunc = null);

        ExecuteResponse FinishQuoteRequest(Guid premiumRequestId);

        QuoteResponse GetResponseFromExistingPampRequest(Guid requestId);

        Dictionary<MetalType, decimal> GetPampPriceForMetalsFromQuoteResponse(QuoteResponse quoteResponse);

        Dictionary<MetalType, decimal> GetPampPricePerOneOzForMetals(IEnumerable<MetalQuantity> metalQuantities,
            Dictionary<MetalType, decimal> metalPriceMaps);

    }

    [ServiceConfiguration(typeof(BullionTradingService))]
    [ServiceConfiguration(typeof(IBullionTradingService))]
    public class BullionTradingService : DbContextDisposable<PremiumRequestDbContext>, IBullionTradingService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BullionTradingService));

        private readonly Lazy<IPricingAndTradingService> PricingAndTradingService = new Lazy<IPricingAndTradingService>(() => ServiceLocator.Current.GetInstance<IPricingAndTradingService>());

        private readonly IContentLoader _contentLoader;

        public BullionTradingService(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        protected virtual AuthenticationDetails AuthenticationDetails
        {
            get
            {
                var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
                if (startPage == null) return new AuthenticationDetails();

                return new AuthenticationDetails
                {
                    BaseUrl = startPage.PampApiUrl,         
                    Password = startPage.PampApiPassword,   
                    UserId = startPage.PampApiUsername      
                };
            }
        }

        public QuoteResponse RequestPampForQuote(
            IEnumerable<MetalQuantity> metalQuantities,
            DealerTradeSide dealerTradeSide = DealerTradeSide.PampSells,
            Func<IAmPremiumVariant, decimal> getQuantityInOzFunc = null)
        {
            return RequestPampForQuote(null, metalQuantities, dealerTradeSide, getQuantityInOzFunc);
        }
        public QuoteResponse RequestPampForQuote(
            CustomerContact customerContact,
            IEnumerable<MetalQuantity> metalQuantities,
            DealerTradeSide dealerTradeSide = DealerTradeSide.PampSells,
            Func<IAmPremiumVariant, decimal> getQuantityInOzFunc = null)
        {
            try
            {
                if (metalQuantities.IsNullOrEmpty()) return null;

                var requestId = Guid.NewGuid();
                var quoteRequest = new PremiumRequest()
                {
                    Id = requestId,
                    ReferenceNumber = $"TRM-{requestId.ToString("N")}",
                    SerializedMetalQuantities = JsonConvert.SerializeObject(metalQuantities),
                    DealerTradeSide = dealerTradeSide.ToString(),
                    CurrentStatus = OrderStatus.InProgress.ToString()
                };

                quoteRequest.ActionHistorical.Add(new PremiumRequestHistorical()
                {
                    Command = RequestCommand.PreRequestForQuote,
                    CreatedDate = DateTime.UtcNow,
                    Id = Guid.NewGuid()
                });

                if (customerContact != null || (CustomerContext.Current != null && CustomerContext.Current.CurrentContact != null))
                {
                    var customerInfo = new Dictionary<string, string>();
                    var contact = customerContact ?? CustomerContext.Current.CurrentContact;
                    customerInfo[CustomFields.BullionCustomerFullName] = contact.FullName;
                    customerInfo[CustomFields.BullionCustomerEmailAddress] = contact.Email;
                    if (contact.ContactOrganization != null)
                    {
                        customerInfo[CustomFields.BullionCurrentCustomerOrganization] = contact.ContactOrganization.Name;
                    }

                    quoteRequest.CustomerInfo = JsonConvert.SerializeObject(customerInfo);
                }

                // Save to DB
                quoteRequest = context.PremiumRequestData.Add(quoteRequest);
                context.SaveChanges();

                // Request for Quote
                return MakeRequestForQuote(quoteRequest);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        public virtual QuoteResponse GetResponseFromExistingPampRequest(Guid requestId)
        {
            try
            {
                var quoteRequest = context.PremiumRequestData.FirstOrDefault(x => x.Id.Equals(requestId));

                var actionHistorical = quoteRequest?.ActionHistorical.FirstOrDefault(x => x.Command.Equals(RequestCommand.RequestForQuote));
                if (actionHistorical == null) return null;

                return JsonConvert.DeserializeObject<QuoteResponse>(actionHistorical.SerializedOutputDto);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        private QuoteResponse MakeRequestForQuote(PremiumRequest quoteRequest)
        {
            var quoteRequestInputDto = new QuoteRequest
            {
                CustomerReferenceNumber = quoteRequest.ReferenceNumber,
                MetalQuantities = quoteRequest.MetalQuantities.ToList(),
                RequestId = quoteRequest.Id.ToString(),
                TradeSide = (DealerTradeSide)Enum.Parse(typeof(DealerTradeSide), quoteRequest.DealerTradeSide)
            };

            var quoteResponseAwaiter = Task.Run(() =>
            {
                return PricingAndTradingService.Value.RequestForQuote(new RequestForQuote
                {
                    AuthenticationDetails = AuthenticationDetails,
                    Request = quoteRequestInputDto
                });
            }).ConfigureAwait(false).GetAwaiter();
            var quoteResponse = quoteResponseAwaiter.GetResult();

            quoteRequest.CurrentStatus = OrderStatus.AwaitingExchange.ToString();

            quoteRequest.ActionHistorical.Add(new PremiumRequestHistorical()
            {
                Command = RequestCommand.RequestForQuote,
                CreatedDate = DateTime.UtcNow,
                SerializedInputDto = JsonConvert.SerializeObject(quoteRequestInputDto),
                SerializedOutputDto = JsonConvert.SerializeObject(quoteResponse),
                Id = Guid.NewGuid()
            });
            context.SaveChanges();
            quoteResponse.QuoteDtoId = quoteRequest.Id;
            return quoteResponse;
        }

        public ExecuteResponse FinishQuoteRequest(Guid requestId)
        {
            try
            {
                var quoteRequest = context.PremiumRequestData.FirstOrDefault(x => x.Id.Equals(requestId));
                if (quoteRequest == null) return null;

                return FinishQuoteRequest(quoteRequest);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        private ExecuteResponse FinishQuoteRequest(PremiumRequest quoteRequest)
        {
            if (quoteRequest == null)
            {
                return null;
            }

            try
            {
                var serializedOutputDto = context.PremiumRequestHistoricalData
                    .Where(x => x.PremiumRequestId == quoteRequest.Id)
                    .OrderByDescending(x=>x.CreatedDate)
                    .First().SerializedOutputDto;

                if (serializedOutputDto == null) return null;

                var quoteResponse = JsonConvert.DeserializeObject<QuoteResponse>(serializedOutputDto);
                var execInputDto = new ExecuteOnQuoteRequest
                {
                    QuoteId = quoteResponse.QuoteId
                };

                var quoteResponseAwaiter = Task.Run(() =>
                    {
                        return PricingAndTradingService.Value.ExecuteOnQuote(new ExecuteOnQuote
                        {
                            AuthenticationDetails = AuthenticationDetails,
                            Request = new ExecuteOnQuoteRequest
                            {
                                QuoteId = quoteResponse.QuoteId
                            }
                        });
                    }).ConfigureAwait(false).GetAwaiter();
                var execRet = quoteResponseAwaiter.GetResult();

                quoteRequest.ActionHistorical.Add(new PremiumRequestHistorical()
                {
                    Command = RequestCommand.FinishQuoteRequest,
                    CreatedDate = DateTime.UtcNow,
                    SerializedInputDto = JsonConvert.SerializeObject(execInputDto),
                    SerializedOutputDto = JsonConvert.SerializeObject(execRet),
                    Id = Guid.NewGuid()
                });
                if (execRet.Result.Success)
                    quoteRequest.CurrentStatus = OrderStatus.Completed.ToString();

                context.SaveChanges();

                return execRet;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        #region Helper methods

        public Dictionary<MetalType, decimal> GetPampPriceForMetalsFromQuoteResponse(QuoteResponse quoteResponse)
        {
            if (quoteResponse?.MetalPriceMap == null) return null;

            var result = new Dictionary<MetalType, decimal>();
            var mapping = GetMetaTypeDescriptionMap();
            foreach (var item in quoteResponse.MetalPriceMap)
            {
                var desc = item.Key.Contains("/") ? item.Key.Split('/')[0] : item.Key;
                if (!mapping.ContainsValue(desc))
                {
                    continue;
                }
                var metalType = mapping.First(x => x.Value == desc).Key;
                var totalPrice = item.Value;
                result.Add(metalType, totalPrice);
            }
            return result;
        }

        public Dictionary<MetalType, decimal> GetPampPricePerOneOzForMetals(IEnumerable<MetalQuantity> metalQuantities, Dictionary<MetalType, decimal> metalPriceMaps)
        {
            var result = new Dictionary<MetalType, decimal>();
            foreach (var metaPriceMap in metalPriceMaps)
            {
                MetalType metalType = metaPriceMap.Key;
                var totalOzQuantity = metalQuantities.FirstOrDefault(x => x.Metal == metalType).QuantityInOz;
                var pricePerOneOz = Math.Round(metaPriceMap.Value / totalOzQuantity, 2);
                result.Add(metalType, pricePerOneOz);
            }
            return result;
        }

        private Dictionary<MetalType, string> GetMetaTypeDescriptionMap()
        {
            var result = new Dictionary<MetalType, string>();
            var enumType = typeof(MetalType);

            foreach (var member in enumType.GetMembers())
            {
                var descAttr = member.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
                if (descAttr == null) continue;

                MetalType enumValue;
                if (!Enum.TryParse(member.Name, out enumValue)) { continue; }
                result.Add(enumValue, descAttr.Description);
            }
            return result;
        }

        #endregion
    }
}