using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Web;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders.Managers;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Models;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers
{
    public class ShippingMethodHelper : IAmShippingMethodHelper
    {
        private readonly IAmShippingManagerHelper _shippingManager;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly LocalizationService _localizationService;
        private readonly IAmMarketHelper _marketHelper;
        private readonly IContentLoader _contentLoader;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IOrderRepository _orderRepository;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public ShippingMethodHelper(
            IOrderRepository orderRepository,
            IAmShippingManagerHelper shippingManager,
            IOrderGroupCalculator orderGroupCalculator,
            IAmInventoryHelper inventoryHelper,
            LocalizationService localizationService,
            IAmMarketHelper marketHelper,
            IContentLoader contentLoader, IAmBullionContactHelper bullionContactHelper)
        {
            _shippingManager = shippingManager;
            _inventoryHelper = inventoryHelper;
            _localizationService = localizationService;
            _marketHelper = marketHelper;
            _contentLoader = contentLoader;
            _bullionContactHelper = bullionContactHelper;
            _orderGroupCalculator = orderGroupCalculator;
            _orderRepository = orderRepository;
        }

        public ShippingMethodSummary GetShippingMethodForDeliverFromVault(CustomerContact contact, decimal amount, decimal weight, string currencyCode)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage == null) return null;

            var bullionAddress = _bullionContactHelper.GetBullionAddress(contact);
            if (bullionAddress == null) return null;

            var market = _marketHelper.GetMarketFromCountryCode(bullionAddress.CountryCode);
            if (market == null) return null;

            var countryId = CountryManager.GetCountry(bullionAddress.CountryCode, false).Country.FirstOrDefault()?.CountryId ?? 0;

            var availableShippingMethods = GetAvailableShippingMethods(amount, market, countryId, true, DateTime.Now, currencyCode);

            var bullionShippingMethod = FilterShippingMethodsByWeight(availableShippingMethods, weight);

            return bullionShippingMethod.Where(x => startPage.BullionShippingMethodList != null && startPage.BullionShippingMethodList.Contains(x.Name))
                .OrderBy(x=>x.DeliveryCostDecimal)
                .FirstOrDefault();
        }

        public List<ShippingMethodSummary> GetAvailableShippingMethods(decimal amount, IMarket market, int countryId, bool allItemsInStock, DateTime currentDate, string currency = null, bool isIncludeGifting = false)
        {

            var shippingMethods = _shippingManager.GetShippingMethodsByMarket(market.MarketId.Value, false);

            var availableShippingMethods = new List<ShippingMethodSummary>();
            foreach (var method in shippingMethods)
            {
                if (method.ForGiftingOnly && !isIncludeGifting) { continue; }
                //is Active
                if (!method.IsActive) { continue; }

                //Available days
                if (!method.AvailableDays.Contains(DateTime.Now.DayOfWeek)) { continue; }

                //Min Order Value
                if (method.MinOrderValue > 0 && method.MinOrderValue > amount) { continue; }

                //Max Order Value
                if (method.MaxOrderValue > 0 && method.MaxOrderValue < amount) { continue; }

                //Countries
                if (countryId > 0 && method.UnavailableCountries.Contains(countryId)) { continue; }

                //Stock
                if (method.AllProductsInStock && allItemsInStock != true)
                { continue; }

                //Time of day cut off
                if (!string.IsNullOrWhiteSpace(method.CutOffTime))
                {
                    TimeSpan cutOffTime;
                    if (!TimeSpan.TryParse(method.CutOffTime, out cutOffTime) || cutOffTime < currentDate.TimeOfDay) continue;
                }

                //Currency not map cart currency
                if (!string.IsNullOrEmpty(currency) && !method.Currency.Equals(currency)) { continue; }

                //Translate the display name - try catch for unit tests 
                try
                {
                    method.DisplayMessage = _localizationService.GetStringByCulture(string.Format(StringResources.DeliveryMethodMessage, method.Name),
                        method.DisplayMessage, ContentLanguage.PreferredCulture);
                }
                catch (Exception)
                {
                    // ignored
                }


                availableShippingMethods.Add(method);
            }

            return availableShippingMethods.OrderBy(m => m.SortOrder).ToList();
        }

        private List<ShippingMethodSummary> GetAvailableShippingMethods(IOrderGroup orderGroup, string countryCode, CustomerContact customerContact = null)
        {
            var countryId = CountryManager.GetCountry(countryCode, false).Country.FirstOrDefault()?.CountryId ?? 0;
            var market = _marketHelper.GetMarketFromCountryCode(countryCode);
            var allItemsInStock = GetInStockStatus(orderGroup, customerContact);
            var giftingItems = HasGiftingItem(orderGroup);
            var totalWithoutDeliveryDecimal = GetTotalWithoutDeliveryDecimal(orderGroup);

            var availableMethods = GetAvailableShippingMethods(totalWithoutDeliveryDecimal, market, countryId, allItemsInStock, DateTime.Now, orderGroup.Currency, giftingItems);

            return availableMethods;
        }

        public List<ShippingMethodSummary> GetAvailableShippingMethodsForConsumerCart(IOrderGroup orderGroup, string countryCode)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage == null) return new List<ShippingMethodSummary>();

            var availableMethods = GetAvailableShippingMethods(orderGroup, countryCode);

            return availableMethods.Where(x => startPage.ConsumerShippingMethodList != null && startPage.ConsumerShippingMethodList.Contains(x.Name)).ToList();
        }

        //Fix bug RM-1558 Postage glitch allowing customer to checkout with lower delivery charge
        public bool ValidateShipping(IOrderGroup orderGroup)
        {
            var shipment = orderGroup.GetFirstShipment() ?? orderGroup.CreateShipment();

            //if Shipping Address is not set -> shipping Method should be not set
            if (shipment.ShippingAddress == null) return true;

            // Try getting current country code
            var countryCode = shipment.ShippingAddress?.CountryCode ?? "GBR";
            var shippingMethods = GetAvailableShippingMethods(orderGroup, countryCode);
            if (!shippingMethods.Any()) return true;

            // The current shipping method is not in the new available list
            if (shippingMethods.Any(x => x.Id == shipment.ShippingMethodId.ToString())) return true;

            // Update new shipping method
            shipment.ShippingMethodId = Guid.Parse(shippingMethods.First().Id);
            orderGroup.ApplyDiscounts();
            _orderRepository.Save(orderGroup);
            return false;
        }

        #region Bullion Shipping Method

        public ShippingMethodSummary GetBullionDefaultShippingMethod(IOrderGroup orderGroup, string countryCode, bool isToVault)
        {
            return GetBullionDefaultShippingMethod(orderGroup, countryCode, isToVault, null);
        }
        public ShippingMethodSummary GetBullionDefaultShippingMethod(IOrderGroup orderGroup, string countryCode, bool isToVault, CustomerContact customerContact)
        {
            var bullionShippingMethods = GetAvailableShippingMethodsForBullionCart(orderGroup, countryCode, isToVault, customerContact);
            return bullionShippingMethods.OrderBy(x => x.DeliveryCostDecimal).FirstOrDefault();
        }

        private IEnumerable<ShippingMethodSummary> GetAvailableShippingMethodsForBullionCart(IOrderGroup orderGroup, string countryCode, bool isToVault, CustomerContact customerContact)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage == null) return new List<ShippingMethodSummary>();

            var availableMethods = GetAvailableShippingMethods(orderGroup, countryCode, customerContact);

            var totalWeight = GetTotalWeightOfBullionVariant(orderGroup);

            var bullionShippingMethod = FilterShippingMethodsByWeight(availableMethods, totalWeight);

            if (isToVault)
            {
                return bullionShippingMethod.Where(x => startPage.VaultShippingMethodList != null && startPage.VaultShippingMethodList.Contains(x.Name)).ToList();
            }

            return bullionShippingMethod.Where(x => startPage.BullionShippingMethodList != null && startPage.BullionShippingMethodList.Contains(x.Name)).ToList();
        }

        private IEnumerable<ShippingMethodSummary> FilterShippingMethodsByWeight(IEnumerable<ShippingMethodSummary> shippingMethods, decimal weight)
        {
            var result = new List<ShippingMethodSummary>();
            foreach (var method in shippingMethods)
            {
                if (method.MinWeight > 0 && method.MinWeight > weight) { continue; }

                if (method.MaxWeight > 0 && method.MaxWeight < weight) { continue; }

                result.Add(method);
            }

            return result.OrderBy(m => m.SortOrder).ToList();
        }

        private decimal GetTotalWithoutDeliveryDecimal(IOrderGroup orderGroup)
        {
            var totals = _orderGroupCalculator.GetOrderGroupTotals(orderGroup);
            return totals.Total.Amount - totals.ShippingTotal.Amount;
        }

        private bool GetInStockStatus(IOrderGroup orderGroup, CustomerContact customerContact)
        {
            if (orderGroup == null) return false;
            var allLineItems = orderGroup.GetAllLineItems();
            return allLineItems
                .Select(x => x.GetEntryContent() as TrmVariant)
                .Where(variant => variant != null)
                .Select(x => _inventoryHelper.GetStockSummary(x.ContentLink, customerContact))
                .Where(stockSummary => stockSummary != null)
                .All(s => s.Status == Enums.eStockStatus.InStock || s.Status == Enums.eStockStatus.LowStock);
        }

        private bool HasGiftingItem(IOrderGroup orderGroup)
        {
            if (orderGroup == null) return false;
            var allLineItems = orderGroup.GetAllLineItems();
            return allLineItems
                .Select(x => x.GetEntryContent() as TrmVariant)
                .Where(variant => variant != null)
                .Any(x => x.IsGifting);
        }

        private decimal GetTotalWeightOfBullionVariant(IOrderGroup orderGroup)
        {
            var allLineItems = orderGroup.GetFirstShipment()?.LineItems;

            if (allLineItems == null) return 0;

            return allLineItems.Select(x => new
            {
                Variant = x.Code.GetVariantByCode() as PreciousMetalsVariantBase,
                x.Quantity
            }).Where(x => x.Variant != null && !(x.Variant is IAmInvestmentVariant)).Sum(x => (decimal)x.Variant.Weight * x.Quantity);
        }

        public void UpdateBullionShippingMethod(IOrderGroup cart, int? shipmentId)
        {
            UpdateBullionShippingMethod(cart, shipmentId, null);
        }
        public void UpdateBullionShippingMethod(IOrderGroup cart, int? shipmentId, CustomerContact customerContact)
        {
            var firstShipment = cart.GetFirstShipment();
            var secondShipment = cart.GetSecondShipment();
            if (shipmentId != null && firstShipment != null && shipmentId == firstShipment.ShipmentId)
            {
                //Update shipping method to delivery
                var shippingMethodForDelivery = GetBullionDefaultShippingMethod(cart, firstShipment.ShippingAddress?.CountryCode, false, customerContact);
                firstShipment.ShippingMethodId = shippingMethodForDelivery != null ? Guid.Parse(shippingMethodForDelivery.Id) : Guid.Empty;

            }
            else if (shipmentId != null && secondShipment != null && shipmentId == secondShipment.ShipmentId)
            {
                //Update shipping method to vault
                var shippingMethodForVault = GetBullionDefaultShippingMethod(cart, secondShipment.ShippingAddress?.CountryCode, true, customerContact);
                secondShipment.ShippingMethodId = shippingMethodForVault != null ? Guid.Parse(shippingMethodForVault.Id) : Guid.Empty;
            }

            if (firstShipment != null && !firstShipment.LineItems.Any())
            {
                firstShipment.ShippingMethodId = Guid.Empty;
            }

            if (secondShipment != null && !secondShipment.LineItems.Any())
            {
                secondShipment.ShippingMethodId = Guid.Empty;
            }
        }

        #endregion
    }
}