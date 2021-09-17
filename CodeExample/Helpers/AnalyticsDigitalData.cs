using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Find.Cms;
using EPiServer.Framework.Cache;
using EPiServer.Web.Routing;
using Hephaestus.CMS.ViewModels;
using Mediachase.Commerce.Customers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TRM.Web.AnalyticsDataLayer;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DataLayer;
using TRM.Web.Models.ViewModels;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Models.ViewModels.Cart;
using TRM.Web.Services;
using static TRM.Shared.Constants.Enums;
using static TRM.Shared.Constants.StringConstants;
using CustomFieldConstants = TRM.Shared.Constants.StringConstants.CustomFields;

namespace TRM.Web.Helpers
{
    public class AnalyticsDigitalData : IAnalyticsDigitalData
    {
        private readonly CustomerContext _customerContext;
        private readonly IContentLoader _contentLoader;
        private readonly IAmContactHelper _contactHelper;
        private readonly IAmOrderHelper _orderHelper;
        private readonly IPageRouteHelper _pageRouteHelper;
        private readonly IContentRouteHelper _contentRouteHelper;
        private readonly IMetalPriceService _metalPriceService;
        private readonly IAmCurrencyHelper _iamCurrencyHelper;
        private readonly ISynchronizedObjectInstanceCache _synchronizedObjectInstanceCache;
        private readonly IAmInventoryHelper _inventoryHelper;
        private const string Affiliation = "Online Store";
        private const string Delivery = "Delivery";
        private const string Vault = "Vault";
        private const string Commemorative = "Commemorative";

        public AnalyticsDigitalData(
            CustomerContext customerContext,
            IContentLoader contentLoader,
            IAmContactHelper contactHelper,
            IAmOrderHelper orderHelper,
            IPageRouteHelper pageRouteHelper,
            IContentRouteHelper contentRouteHelper,
            IMetalPriceService metalPriceService,
            IAmCurrencyHelper iamCurrencyHelper,
            ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache,
            IAmInventoryHelper inventoryHelper)
        {
            _customerContext = customerContext;
            _contentLoader = contentLoader;
            _contactHelper = contactHelper;
            _pageRouteHelper = pageRouteHelper;
            _contentRouteHelper = contentRouteHelper;
            _metalPriceService = metalPriceService;
            _orderHelper = orderHelper;
            _iamCurrencyHelper = iamCurrencyHelper;
            _synchronizedObjectInstanceCache = synchronizedObjectInstanceCache;
            _inventoryHelper = inventoryHelper;
        }
        public AnalyticsDigitalDataModel GenerateMeasuringProductImpressionsJson(IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> model)
        {
            if (model == null || (model.CurrentPage == null && model.ViewModel == null) || model.ViewModel.Entries == null || !model.ViewModel.Entries.Any()) return null;

            var analyticsData = new MeasuringProductImpression();

            var products = new ProductWithListAndPosition[model.ViewModel.Entries.Count];
            for (var index = 0; index < model.ViewModel.Entries.Count; index++)
            {
                var entry = model.ViewModel.Entries[index];

                TrmVariant trmVariant;
                _contentLoader.TryGet<TrmVariant>(entry.ContentLink, out trmVariant);

                var price = entry?.Price != null ? String.Format("{0:0.00}", entry.Price) : string.Empty;
                products[index] = new ProductWithListAndPosition
                {
                    Name = !string.IsNullOrWhiteSpace(entry?.DisplayName) ? entry.DisplayName.StripHtmlTagsAndSpecialChars() : string.Empty,
                    Id = !string.IsNullOrWhiteSpace(entry.Code) ? entry.Code : string.Empty,
                    Price = price,
                    Brand = !string.IsNullOrWhiteSpace(trmVariant?.BrandDisplayName) ? trmVariant?.BrandDisplayName : Commemorative,
                    Category = !string.IsNullOrWhiteSpace(model?.CurrentPage?.DisplayName) ? model.CurrentPage.DisplayName : string.Empty,
                    List = !string.IsNullOrWhiteSpace(model?.CurrentPage?.DisplayName) ? model.CurrentPage.DisplayName : string.Empty,
                    Position = index + 1,
                    Variant = IdentifyVariant(entry.Code)
                };

            }

            analyticsData.Ecommerce.CurrencyCode = !string.IsNullOrWhiteSpace(model.ViewModel.Entries.FirstOrDefault()?.CurrencyCode) ? model.ViewModel.Entries.FirstOrDefault()?.CurrencyCode : string.Empty;
            analyticsData.Ecommerce.Impressions = products;

            return new AnalyticsDigitalDataModel
            {
                JsonString = JsonConvert.SerializeObject(analyticsData),
                Type = Enums.DataLayer.MeasuringProductImpressions
            };
        }
        public AnalyticsDigitalDataModel GenerateMeasuringViewProductDetailJson(IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> model)
        {
            if (model == null || (model.CurrentPage == null && model.ViewModel == null)) return null;

            var analyticsData = new MeasuringViewProductDetail();
            analyticsData.Ecommerce.Detail.ActionField.List = string.Empty;
            analyticsData.Ecommerce.Detail.Products = new Product[]
            {
                new Product
                {
                    Name = !string.IsNullOrWhiteSpace(model?.CurrentPage?.DisplayName) ? model.CurrentPage.DisplayName.StripHtmlTagsAndSpecialChars() : model.CurrentPage.MetaTitle,
                    Id = !string.IsNullOrWhiteSpace(model?.CurrentPage?.Code) ? model.CurrentPage.Code : string.Empty,
                    Price = !string.IsNullOrWhiteSpace(model?.ViewModel?.PriceString) ? model.ViewModel.PriceString : string.Empty,
                    Brand = !string.IsNullOrWhiteSpace(model?.CurrentPage?.BrandDisplayName) ? model.CurrentPage.BrandDisplayName : string.Empty,
                    Category = !string.IsNullOrWhiteSpace(model?.CurrentPage?.Category) ? model.CurrentPage.Category : string.Empty,
                }
            };

            return new AnalyticsDigitalDataModel
            {
                JsonString = JsonConvert.SerializeObject(analyticsData),
                Type = Enums.DataLayer.MeasuringViewsOfProductDetails
            };
        }
        public DataLayerVariantSchema GenerateVariantSchema(EntryPartialViewModel entryPartialViewModel, int? index = null)
        {
            var response = new DataLayerVariantSchema();
            if (entryPartialViewModel != null)
            {
                _contentLoader.TryGet(entryPartialViewModel.ContentLink, out TrmVariant trmVariant);
                response.Title = !string.IsNullOrWhiteSpace(entryPartialViewModel.Title) ? entryPartialViewModel.Title : !string.IsNullOrWhiteSpace(entryPartialViewModel.DisplayName) ? entryPartialViewModel.DisplayName.StripHtmlTagsAndSpecialChars() : string.Empty;
                response.Code = !string.IsNullOrWhiteSpace(entryPartialViewModel.Code) ? entryPartialViewModel.Code : string.Empty;
                response.Price = !string.IsNullOrWhiteSpace(entryPartialViewModel.PriceString) ? entryPartialViewModel.PriceString : string.Empty;
                response.Brand = !string.IsNullOrWhiteSpace(trmVariant?.BrandDisplayName) ? trmVariant.BrandDisplayName : !string.IsNullOrWhiteSpace(trmVariant?.Brand) ? trmVariant.Brand : string.Empty;
                response.Quantity = entryPartialViewModel.StockSummary?.PurchaseAvailableQuantity != null ? entryPartialViewModel.StockSummary.PurchaseAvailableQuantity.ToString() : string.Empty;
                response.StockStatus = entryPartialViewModel.StockSummary?.Status != null ? entryPartialViewModel.StockSummary.Status.ToString() : string.Empty;
                response.Position = index.HasValue ? index.Value.ToString() : string.Empty;
            }

            return response;
        }

        public async Task<AnalyticsDigitalDataModel> GetCustomerJsonAsync()
        {
            var customerData = new Customer();
            var currentContact = _customerContext.CurrentContact;

            if (currentContact != null)
            {
                var bullionCustomerBalance = currentContact.Properties[CustomFieldConstants.BullionCustomerEffectiveBalance]?.Value?.ToString();

                var creditLimit = currentContact.Properties[CustomFieldConstants.CreditLimitFieldName]?.Value?.ToString();
                var creditUsed = currentContact.Properties[CustomFieldConstants.CreditUsedEPiFieldName]?.Value?.ToString();

                decimal creditLimitValue = 0;
                decimal.TryParse(creditLimit, out creditLimitValue);
                decimal creditUsedValue = 0;
                decimal.TryParse(creditUsed, out creditUsedValue);

                var contactByEmail = currentContact.Properties[CustomFieldConstants.ContactByEmail]?.Value?.ToString();
                var contactByPost = currentContact.Properties[CustomFieldConstants.ContactByPost]?.Value?.ToString();
                var contactByPhone = currentContact.Properties[CustomFieldConstants.ContactByPhone]?.Value?.ToString();

                var group = currentContact.Properties[CustomFieldConstants.CustomerGroup]?.Value?.ToString();
                int groupValue = 0;
                Int32.TryParse(group, out groupValue);
                var groupName = groupValue != 0 ? Enum.GetName(typeof(CustomGroupsEnum), groupValue) : string.Empty;

                var lifetimeValue = currentContact.Properties[CustomFieldConstants.LifetimeValue]?.Value?.ToString();

                customerData.CustomerDetails = new CustomerDetails
                {
                    ObsAccountNumber = currentContact.Properties[CustomFieldConstants.ObsAccountNumber]?.Value?.ToString(),
                    BullionObsAccountNumber = currentContact.Properties[CustomFieldConstants.BullionObsAccountNumber]?.Value?.ToString(),
                    CustomerType = IdentifyCustomerType(currentContact.Properties[CustomFieldConstants.CustomerType]?.Value?.ToString()),
                    LifetimeValue = !string.IsNullOrWhiteSpace(lifetimeValue) ? String.Format("{0:0.00}", lifetimeValue.ToDecimal()) : "0",
                    Balance = !string.IsNullOrWhiteSpace(bullionCustomerBalance) ? Convert.ToDecimal(bullionCustomerBalance) : 0,
                    Credit = creditLimitValue - creditUsedValue,
                    Group = groupName,
                    Marketing = new Marketing
                    {
                        Email = !string.IsNullOrWhiteSpace(contactByEmail) ? Convert.ToBoolean(contactByEmail) : false,
                        Post = !string.IsNullOrWhiteSpace(contactByPost) ? Convert.ToBoolean(contactByPost) : false,
                        Phone = !string.IsNullOrWhiteSpace(contactByPhone) ? Convert.ToBoolean(contactByPhone) : false,
                    },
                };
            }

            return new AnalyticsDigitalDataModel
            {
                JsonString = JsonConvert.SerializeObject(customerData),
                Type = Enums.DataLayer.MeasuringPageViews,
            };
        }
        public async Task<AnalyticsDigitalDataModel> GetPageJsonAsync()
        {
            var pageData = new Page();

            var content = _contentRouteHelper.Content;
            if (content != null && content is CatalogContentBase catalogContent)
            {
                var contentType = catalogContent.ContentTypeName();
                pageData.PageDetails = new PageDetails
                {
                    ContentTitle = !string.IsNullOrWhiteSpace(catalogContent.Name) ? catalogContent.Name : string.Empty,
                    ContentType = !string.IsNullOrWhiteSpace(contentType) ? contentType : string.Empty,
                };
            }
            else
            {
                var page = _pageRouteHelper.Page;
                if (page != null)
                {
                    pageData.PageDetails = new PageDetails
                    {
                        ContentTitle = !string.IsNullOrWhiteSpace(page.PageName) ? page.PageName : string.Empty,
                        ContentType = !string.IsNullOrWhiteSpace(page.PageTypeName) ? page.PageTypeName : string.Empty,
                    };
                }
            }

            return new AnalyticsDigitalDataModel
            {
                JsonString = JsonConvert.SerializeObject(pageData),
                Type = Enums.DataLayer.MeasuringPageViews,
            };
        }
        public async Task<AnalyticsDigitalDataModel> GetMarketJsonAsync()
        {
            var marketData = new Market();

            var priceModel = _metalPriceService.BuildPampMetalPriceModel();
            if (priceModel != null)
            {
                var gold = priceModel
                    .PampMetalPriceItems
                    .FirstOrDefault(x => string.Equals(x.Metal.Name, "gold", StringComparison.InvariantCultureIgnoreCase));
                var silver = priceModel
                    .PampMetalPriceItems
                    .FirstOrDefault(x => string.Equals(x.Metal.Name, "silver", StringComparison.InvariantCultureIgnoreCase));
                var platinum = priceModel
                    .PampMetalPriceItems
                    .FirstOrDefault(x => string.Equals(x.Metal.Name, "platinum", StringComparison.InvariantCultureIgnoreCase));

                marketData.MarketDetails = new MarketDetails
                {
                    BuyDetails = new MetalPrices
                    {
                        Gold = gold?.BuyPriceChange.CurrentPrice ?? 0m,
                        Silver = silver?.BuyPriceChange.CurrentPrice ?? 0m,
                        Platinum = platinum?.BuyPriceChange.CurrentPrice ?? 0m,
                    },
                    SellDetails = new MetalPrices
                    {
                        Gold = gold?.SellPriceChange.CurrentPrice ?? 0m,
                        Silver = silver?.SellPriceChange.CurrentPrice ?? 0m,
                        Platinum = platinum?.SellPriceChange.CurrentPrice ?? 0m,
                    }
                };
            }

            return new AnalyticsDigitalDataModel
            {
                JsonString = JsonConvert.SerializeObject(marketData),
                Type = Enums.DataLayer.MeasuringPageViews,
            };
        }
        public async Task<AnalyticsDigitalDataModel> GetEcommerceJsonAsync()
        {
            var ecommerceData = new Ecommerce();

            var customer = _contactHelper.GetCheckoutCustomerContact();
            if (customer != null)
            {
                ecommerceData.EcommerceDetails = new EcommerceDetails
                {
                    CurrencyCode = !string.IsNullOrWhiteSpace(customer.PreferredCurrency) ? customer.PreferredCurrency : string.Empty,
                };
            }

            return new AnalyticsDigitalDataModel
            {
                JsonString = JsonConvert.SerializeObject(ecommerceData),
                Type = Enums.DataLayer.MeasuringPageViews,
            };
        }
        public CheckoutModel GenerateCartInfo(LargeOrderGroupViewModel largeOrderGroupViewModel, CustomerContact customerContact, int step)
        {
            if (largeOrderGroupViewModel == null) return null;

            var analyticsData = InitializeMeasuringCheckout(customerContact, step);
            var isEuroCurrency = analyticsData.Ecommerce.CurrencyCode.ToLower().Contains("eur");

            var actionFieldDetail = new ActionFieldDetail
            {
                Coupon = IdentifyCoupons(largeOrderGroupViewModel?.Promotions?.Select(x => x.Code)),
                Affiliation = Affiliation,
            };

            analyticsData.Ecommerce.Basket.Products = GenerateProductsForCheckout(largeOrderGroupViewModel.Shipments, isEuroCurrency).ToArray();

            return new CheckoutModel
            {
                AnalyticsDigitalDataModel = new AnalyticsDigitalDataModel
                {
                    JsonString = JsonConvert.SerializeObject(analyticsData),
                    Type = Enums.DataLayer.MeasuringViewsOfProductDetails
                },
                ActionFieldDetail = actionFieldDetail
            };
        }

        private MeasuringCheckout InitializeMeasuringCheckout(CustomerContact customerContact, int step)
        {
            var analyticsData = new MeasuringCheckout();
            if (customerContact == null)
            {
                analyticsData.Ecommerce.CurrencyCode = _iamCurrencyHelper.GetDefaultCurrencyCode();
                analyticsData.Ecommerce.Basket.ActionField.Step = step;
            }
            else
            {
                analyticsData.Ecommerce.CurrencyCode = _iamCurrencyHelper.GetDefaultCurrencyCode(customerContact);
                analyticsData.Ecommerce.Basket.ActionField.Step = step;
            }

            return analyticsData;
        }
        private string IdentifyVariant(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;

            var metalType = string.Empty;
            var variant = code.GetVariantByCode();
            var trmVariant = variant as TrmVariant;

            var pureMetal = trmVariant?.PureMetalTypeDisplayName?.ToString();
            if (trmVariant != null)
            {
                pureMetal = trmVariant.PureMetalTypeDisplayName;
                if (string.IsNullOrWhiteSpace(pureMetal) && !string.IsNullOrWhiteSpace(trmVariant.OtherSpecificationValue))
                {
                    var otherSpcificationValue = trmVariant.OtherSpecificationValue.ToLower();
                    if (otherSpcificationValue.Contains("gold"))
                    {
                        pureMetal = "Gold";
                    }
                    else if (otherSpcificationValue.Contains("silver"))
                    {
                        pureMetal = "silver";
                    }
                    else if (otherSpcificationValue.Contains("platinum"))
                    {
                        pureMetal = "Platinum";
                    }
                }
                if (string.IsNullOrWhiteSpace(pureMetal) && !string.IsNullOrWhiteSpace(trmVariant.Alloy))
                {
                    var alloy = trmVariant.Alloy.ToLower();
                    if (alloy.Contains("gold"))
                    {
                        pureMetal = "Gold";
                    }
                    else if (alloy.Contains("silver"))
                    {
                        pureMetal = "silver";
                    }
                    else if (alloy.Contains("platinum"))
                    {
                        pureMetal = "Platinum";
                    }
                }
                var qulityDisplayName = trmVariant.QualityDisplayName.ToLower();
                if (!string.IsNullOrWhiteSpace(qulityDisplayName) && qulityDisplayName.Contains("brilliant uncirculated"))
                {
                    metalType = $"{pureMetal} {"BU"}";
                }
                else
                {
                    metalType = $"{pureMetal.ToTitleCase()} {qulityDisplayName.ToTitleCase()}";
                }
            }
            if (variant is BarVariant barVariant)
            {
                metalType = $"{barVariant.MetalType.ToString().ToTitleCase()}";
            }

            if (variant is SignatureVariant signatureVariant)
            {
                metalType = $"{signatureVariant.MetalType.ToString().ToTitleCase()}";
            }
            return metalType.Trim();
        }
        private IList<ProductForCheckout> GenerateProductsForCheckout(IEnumerable<ShipmentViewModel> shipments, bool isEuroCurrency = false)
        {
            var products = new List<ProductForCheckout>();
            foreach (var shipment in shipments)
            {
                if (shipment?.CartItems != null && shipment.CartItems.Any())
                {
                    var dimention2 = string.Empty;
                    if (shipment.ShipmentUniqueId.ToLower().Contains("deliver") ||
                        shipment.ShipmentUniqueId.ToLower().Contains("default-1"))
                    {
                        dimention2 = Delivery;
                    }
                    else if (shipment.ShipmentUniqueId.ToLower().Contains("vault"))
                    {
                        dimention2 = Vault;
                    }
                    foreach (var item in shipment.CartItems)
                    {
                        var price = item?.PlacedPriceDecimal != null ? String.Format("{0:0.00}", item.PlacedPriceDecimal) : string.Empty;
                        var quantity = item?.Quantity != null ? float.Parse(String.Format("{0:0.##########}", item.Quantity)) : 0;
                        if (item != null && item.DiscountedUnitPriceDecimal > 0)
                        {
                            price = String.Format("{0:0.00}", item.DiscountedUnitPriceDecimal);
                        }
                        if (item != null && item.BullionCartItem != null)
                        {
                            price = item.BullionCartItem.PricePerUnit != null ? String.Format("{0:0.00}", item?.BullionCartItem?.PricePerUnit) : string.Empty;
                            var quantityOrWeight = item.BullionCartItem.IsSignatureVariant == true ? item.BullionCartItem.Weight : item.BullionCartItem.Quantity;
                            quantity = float.Parse(String.Format("{0:0.##########}", quantityOrWeight));
                        }
                        products.Add(
                            new ProductForCheckout
                            {
                                Name = !string.IsNullOrWhiteSpace(item?.DisplayName) ? item.DisplayName.StripHtmlTagsAndSpecialChars() : string.Empty,
                                Id = !string.IsNullOrWhiteSpace(item?.Code) ? item.Code : string.Empty,
                                Price = isEuroCurrency ? price.Replace(",", ".") : price,
                                Brand = !string.IsNullOrWhiteSpace(item?.BrandDisplayName) ? item.BrandDisplayName : Commemorative,
                                Category = !string.IsNullOrWhiteSpace(item?.CategoryName) ? item.CategoryName : string.Empty,
                                Quantity = quantity,
                                Dimension1 = !string.IsNullOrWhiteSpace(item?.StockSummary?.StatusMessage) ? item.StockSummary.StatusMessage : string.Empty,
                                Dimension2 = dimention2,
                                Variant = IdentifyVariant(item.Code)
                            }
                        );
                    }
                }
            }
            return products;
        }
        public CheckoutModel GenerateCartInfo(MixedOrderGroupViewModel mixedOrderGroupViewModel, CustomerContact customerContact, int step)
        {
            if (mixedOrderGroupViewModel == null) return null;
            var analyticsData = InitializeMeasuringCheckout(customerContact, step);
            var isEuroCurrency = analyticsData.Ecommerce.CurrencyCode.ToLower().Contains("eur");

            var actionFieldDetail = new ActionFieldDetail
            {
                Coupon = IdentifyCoupons(mixedOrderGroupViewModel?.PromotionCodes),
                Affiliation = Affiliation,
                Revenue = FormatRevenuTotal(mixedOrderGroupViewModel, isEuroCurrency),
                Tax = FormatTaxTotal(mixedOrderGroupViewModel),
                Shipping = FormatDeliverTotal(mixedOrderGroupViewModel, isEuroCurrency)
            };

            analyticsData.Ecommerce.Basket.Products = GenerateProductsForCheckout(mixedOrderGroupViewModel.Shipments, isEuroCurrency).ToArray();

            return new CheckoutModel
            {
                AnalyticsDigitalDataModel = new AnalyticsDigitalDataModel
                {
                    JsonString = JsonConvert.SerializeObject(analyticsData),
                    Type = Enums.DataLayer.MeasuringViewsOfProductDetails
                },
                ActionFieldDetail = actionFieldDetail
            };
        }
        public void UpdateEcommerceAnalyticsDataOnConfirmation(OrderConfirmModel model)
        {
            if (model == null) return;

            var cacheKey = $"{_customerContext.CurrentContactId}{_customerContext.CurrentContactName}{GlobalCache.InitializeCart}";
            if (_synchronizedObjectInstanceCache.Get(cacheKey) is CheckoutModel cache)
            {
                if (!string.IsNullOrWhiteSpace(cache.AnalyticsDigitalDataModel.JsonString))
                {
                    var isEuroCurrency = _iamCurrencyHelper.GetDefaultCurrencyCode().ToLower().Contains("eur");
                    if (!string.IsNullOrWhiteSpace(model.Revenue))
                    {
                        cache.ActionFieldDetail.Revenue = ConvertEuroToDecimalStringIfNeeded(model.Revenue, isEuroCurrency);
                    }
                    if (!string.IsNullOrWhiteSpace(model.Tax))
                    {
                        cache.ActionFieldDetail.Tax = ConvertEuroToDecimalStringIfNeeded(model.Tax, isEuroCurrency);
                    }
                    if (!string.IsNullOrWhiteSpace(model.TransactionId))
                        cache.ActionFieldDetail.Id = model.TransactionId;

                    if (!string.IsNullOrWhiteSpace(model.BullionDelivery))
                    {
                        cache.ActionFieldDetail.Shipping = ConvertEuroToDecimalStringIfNeeded(model.BullionDelivery, isEuroCurrency);
                    }
                }
            }
        }

        #region Analytics For Sell Back - Digital Data

        public CheckoutModel GenerateSellBackInfo(SellBullionDefaultLandingViewModel viewModel, CustomerContact customerContact, int step, decimal sellPremiumValue)
        {
            if (viewModel == null) return null;

            var variant = viewModel.SellVariant.Code.GetVariantByCode();
            var stockSummary = _inventoryHelper.GetStockSummary(variant.ContentLink);

            var analyticsData = new MeasuringCheckout();
            var products = new List<ProductForCheckout>();

            if (customerContact == null)
            {
                analyticsData.Ecommerce.CurrencyCode = _iamCurrencyHelper.GetDefaultCurrencyCode();
                analyticsData.Ecommerce.Basket.ActionField.Step = step;
            }
            else
            {
                analyticsData.Ecommerce.CurrencyCode = _iamCurrencyHelper.GetDefaultCurrencyCode(customerContact);
                var customerType = customerContact.Properties[CustomFields.CustomerType]?.Value?.ToString();

                analyticsData.Ecommerce.Basket.ActionField.Step = step;
            }
            var isEuroCurrency = analyticsData.Ecommerce.CurrencyCode.ToLower().Contains("eur");

            var brnadName = !string.IsNullOrWhiteSpace(variant?.BrandForProductFeed) ?
                                variant.BrandForProductFeed :
                                !string.IsNullOrWhiteSpace(variant?.BrandDisplayName) ?
                                    variant.BrandDisplayName : string.Empty;

            var price = sellPremiumValue;
            var availableToSell = viewModel?.SellVariant?.AvailableToSell != null ? viewModel.SellVariant.AvailableToSell : 0;
            if (viewModel.SellVariant.BullionType == Enums.BullionVariantType.Signature)
            {
                price = price / availableToSell;
            }

            var strPrice = String.Format("{0:0.00}", price);
            products.Add(new ProductForCheckout
            {
                Name = !string.IsNullOrWhiteSpace(viewModel?.SellVariant?.Title) ? viewModel.SellVariant.Title : string.Empty,
                Id = !string.IsNullOrWhiteSpace(viewModel?.SellVariant?.Code) ? viewModel.SellVariant.Code : string.Empty,
                Price = isEuroCurrency ? strPrice.Replace(",", ".") : strPrice,
                Brand = brnadName,
                Quantity = float.Parse(String.Format("{0:0.##########}", availableToSell)),
                Dimension1 = !string.IsNullOrWhiteSpace(stockSummary?.StatusMessage) ? stockSummary.StatusMessage : string.Empty,
            });

            analyticsData.Ecommerce.Basket.Products = products.ToArray();

            var revenue = sellPremiumValue;
            if (viewModel.SellVariant.BullionType == Enums.BullionVariantType.Coin)
            {
                revenue = sellPremiumValue * viewModel.SellVariant.AvailableToSell;
            }

            var strRevenue = String.Format("{0:0.00}", revenue);
            var actionFieldDetail = new ActionFieldDetail
            {
                Id = viewModel.SellTransactionNumber,
                Affiliation = "Sell Back",
                Revenue = isEuroCurrency ? strRevenue.Replace(",", ".") : strRevenue,
            };

            return new CheckoutModel
            {
                AnalyticsDigitalDataModel = new AnalyticsDigitalDataModel
                {
                    JsonString = JsonConvert.SerializeObject(analyticsData),
                    Type = Enums.DataLayer.MeasuringViewsOfProductDetails
                },
                ActionFieldDetail = actionFieldDetail
            };
        }
        #endregion

        #region Private methods
        private string IdentifyCoupons(IEnumerable<string> promotionCodes)
        {
            if (promotionCodes == null || !promotionCodes.Any()) return string.Empty;

            promotionCodes = promotionCodes.Where(x => !string.IsNullOrWhiteSpace(x));
            if (promotionCodes.Count() == 1) return promotionCodes.FirstOrDefault();

            var responseCodes = string.Empty;
            var listPromotionCodes = new List<string>();
            foreach (var item in promotionCodes)
            {
                var pCodes = item.Split(',').ToList();
                listPromotionCodes.AddRange(pCodes.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            var distinctCodes = listPromotionCodes.Distinct().ToList();

            for (int i = 0; i < distinctCodes.Count; i++)
            {
                responseCodes += distinctCodes[i];
                if (i + 1 != distinctCodes.Count())
                    responseCodes += ",";
            }

            return responseCodes;
        }
        private string FormatIds(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any()) return string.Empty;

            ids = ids.Where(x => !string.IsNullOrWhiteSpace(x));
            if (ids.Count() == 1) return ids.FirstOrDefault();

            return $"{ids.FirstOrDefault()}/{ids.LastOrDefault()}";
        }

        private string IdentifyCustomerType(string customerType)
        {
            if (string.IsNullOrWhiteSpace(customerType)) return string.Empty;

            switch (customerType)
            {
                case CustomerType.Consumer:
                    return AnalyticsConstants.Standard;
                case CustomerType.Bullion:
                    return AnalyticsConstants.Bullion;
                case CustomerType.ConsumerAndBullion:
                    return AnalyticsConstants.StandardBullion;
                default:
                    return AnalyticsConstants.Guest;
            }
        }
        private string FormatRevenuTotal(MixedOrderGroupViewModel mixedOrderGroupViewModel, bool isEuroCurrency = false)
        {
            // Investment
            var intvestmentDeliveryFee = string.Empty;

            var intvestmentSubTotal = !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.InvestmentSubTotal) ?
                    FormatDigitOnlyToString(mixedOrderGroupViewModel.InvestmentSubTotal) : string.Empty;

            var decimalIntvestmentSubTotal = !string.IsNullOrWhiteSpace(intvestmentSubTotal) ? Convert.ToDecimal(ConvertEuroToDecimalStringIfNeeded(intvestmentSubTotal, isEuroCurrency)) : 0;
            decimal decimalTotalIntvestmentDeliveryFee = 0;

            if (mixedOrderGroupViewModel.IsFreeInvestmentDelivery)
            {
                intvestmentDeliveryFee = !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.InvestmentVatWithoutDeliveryFee) ?
                    FormatDigitOnlyToString(mixedOrderGroupViewModel.InvestmentVatWithoutDeliveryFee) : string.Empty;
            }
            else
            {
                intvestmentDeliveryFee = !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.InvestmentVat) ?
                    FormatDigitOnlyToString(mixedOrderGroupViewModel.InvestmentVat) : string.Empty;

                var totalIntvestmentDeliveryFee = !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.TotalInvestmentDelivery) ?
                    FormatDigitOnlyToString(mixedOrderGroupViewModel.TotalInvestmentDelivery) : string.Empty;

                decimalTotalIntvestmentDeliveryFee = !string.IsNullOrWhiteSpace(totalIntvestmentDeliveryFee) ? Convert.ToDecimal(ConvertEuroToDecimalStringIfNeeded(totalIntvestmentDeliveryFee, isEuroCurrency)) : 0;
            }
            var decimalIntvestmentDeliveryFee = !string.IsNullOrWhiteSpace(intvestmentDeliveryFee) ? Convert.ToDecimal(ConvertEuroToDecimalStringIfNeeded(intvestmentDeliveryFee, isEuroCurrency)) : 0;

            // Retails
            var retailSubTotal = !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.RetailSubTotal) ?
                    FormatDigitOnlyToString(mixedOrderGroupViewModel.RetailSubTotal) : string.Empty;

            var decimalretailSubTotal = !string.IsNullOrWhiteSpace(retailSubTotal) ? Convert.ToDecimal(ConvertEuroToDecimalStringIfNeeded(retailSubTotal, isEuroCurrency)) : 0;

            decimal decimalTotalretailDeliveryFee = 0;

            if (!mixedOrderGroupViewModel.IsFreeRetailDelivery)
            {
                var totalretailDeliveryFee = !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.RetailDeliveryTotal) ?
                    FormatDigitOnlyToString(mixedOrderGroupViewModel.RetailDeliveryTotal) : string.Empty;

                decimalTotalretailDeliveryFee = !string.IsNullOrWhiteSpace(totalretailDeliveryFee) ? Convert.ToDecimal(ConvertEuroToDecimalStringIfNeeded(totalretailDeliveryFee, isEuroCurrency)) : 0;
            }

            return String.Format("{0:0.00}", decimalIntvestmentSubTotal + decimalTotalIntvestmentDeliveryFee + decimalIntvestmentDeliveryFee + decimalretailSubTotal + decimalTotalretailDeliveryFee);
        }

        private string FormatDeliverTotal(MixedOrderGroupViewModel mixedOrderGroupViewModel, bool isEuroCurrency = false)
        {
            var investmentDelivery = string.Empty;
            if (!mixedOrderGroupViewModel.IsFreeInvestmentDelivery)
                investmentDelivery = !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.TotalInvestmentDelivery) ? FormatDigitOnlyToString(mixedOrderGroupViewModel.TotalInvestmentDelivery) : string.Empty;

            var retailDelivery = string.Empty;
            if (!mixedOrderGroupViewModel.IsFreeRetailDelivery)
                retailDelivery = !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.RetailDeliveryTotal) ? FormatDigitOnlyToString(mixedOrderGroupViewModel.RetailDeliveryTotal) : string.Empty;

            var decimalInvestmentDelivery = !string.IsNullOrWhiteSpace(investmentDelivery) ? Convert.ToDecimal(ConvertEuroToDecimalStringIfNeeded(investmentDelivery, isEuroCurrency)) : 0;
            var decimalRetailDelivery = !string.IsNullOrWhiteSpace(retailDelivery) ? Convert.ToDecimal(ConvertEuroToDecimalStringIfNeeded(retailDelivery, isEuroCurrency)) : 0;

            return String.Format("{0:0.00}", decimalInvestmentDelivery + decimalRetailDelivery);
        }
        private string FormatTaxTotal(MixedOrderGroupViewModel mixedOrderGroupViewModel)
        {
            if (!mixedOrderGroupViewModel.IsFreeInvestmentDelivery)
                return !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.InvestmentVat) ?
                        FormatDigitOnlyToString(mixedOrderGroupViewModel.InvestmentVat) : string.Empty;

            return !string.IsNullOrWhiteSpace(mixedOrderGroupViewModel?.InvestmentVatWithoutDeliveryFee) ?
                        FormatDigitOnlyToString(mixedOrderGroupViewModel.InvestmentVatWithoutDeliveryFee) : string.Empty;
        }

        private string ConvertEuroToDecimalStringIfNeeded(string stringValue, bool isEuroCurrency = false)
        {
            if (isEuroCurrency == false) return stringValue;

            var convertedValue = FormatDigitOnlyToString(stringValue);
            return convertedValue.Replace(",", ".");
        }

        private string FormatDigitOnlyToString(string mixedString)
        {
            if (string.IsNullOrWhiteSpace(mixedString)) return mixedString;
            return Regex.Replace(mixedString, @"[^0-9.,]", string.Empty);
        }
        #endregion Private methods
    }
}