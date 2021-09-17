using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.Helpers;
using Hephaestus.ContentTypes.Business.Extensions;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TRM.Web.Constants;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Extentions
{
    public static class TrmVariantExt
    {
        public static Injected<IAmStoreHelper> StoreHelper;
        public static Injected<IAmEntryHelper> EntryHelper;
        public static Injected<IAmAssetHelper> AssetHelper;
        public static Injected<CustomerContext> CustomerContext;
        private static ILogger Logger = LogManager.GetLogger(typeof(TrmVariantExt));
        private static readonly LocalizationService LocalizationService = LocalizationService.Current;

        public static string GetDefaultAssetUrl(this TrmVariant thisVariant)
        {
            try
            {
                return AssetHelper.Service.GetDefaultAssetUrl(thisVariant.ContentLink);
            }
            catch (Exception e)
            {
                Logger.Critical("Unable to return default asset url for product " + thisVariant.Code, e);
                return string.Empty;
            }
        }

        public static IEnumerable<int> GetAssociatedReferences(this TrmVariant thisVariant)
        {
            try
            {
                return EntryHelper.Service.GetAssociatedReferences(thisVariant.ContentLink);
            }
            catch (Exception e)
            {
                Logger.Critical("Unable to return associated references for product " + thisVariant.Code, e);
                return new List<int>();
            }
        }

        public static Money DisplayPrice(this TrmVariant thisVariant)
        {
            try
            {
                var discountPrice = StoreHelper.Service.GetDiscountPrice(thisVariant.ContentLink, 1);
                if (discountPrice != null) return discountPrice.Money;
                return EmptyAmount();
            }
            catch (Exception e)
            {
                Logger.Critical("Unable to return a price for product " + thisVariant.Code, e);
                return EmptyAmount();
            }
        }

        public static TrmVariant GetVariantByCode(this string code)
        {
            try
            {
                var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
                var productReference = referenceConverter.GetContentLink(code);

                if (ContentReference.IsNullOrEmpty(productReference)) return null;

                TrmVariant trmVariant;
                contentLoader.TryGet<TrmVariant>(productReference, out trmVariant);
                return trmVariant;
            }
            catch (Exception e)
            {
                Logger.Critical("Product Reference is not available " + code, e);
                return null;
            }
        }

        public static Enums.BullionVariantType GetBullionVariantType(this TrmVariant thisVariant)
        {
            if (thisVariant is SignatureVariant) return Enums.BullionVariantType.Signature;
            if (thisVariant is BarVariant) return Enums.BullionVariantType.Bar;
            if (thisVariant is CoinVariant) return Enums.BullionVariantType.Coin;
            return Enums.BullionVariantType.None;
        }

        public static Money EmptyAmount()
        {
            return new Money(0M, Currency.EUR);
        }

        public static bool IsItemNotInGbpCurrency(this TrmVariant trmVariant)
        {
            //Only show on TrmVariant Landing Page

            if (trmVariant != null && !trmVariant.IsConsumerProducts) return false;

            var customer = CustomerContext.Service.CurrentContact;
            if (customer == null) return false;
            var bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            var currencyCode = bullionContactHelper.GetDefaultCurrencyCode(customer);

            return bullionContactHelper.IsBullionAccount(customer) && !currencyCode.Equals(Currency.GBP);
        }

        public static decimal ParseEpiQtyToAx(this string variantCode, decimal epiQty)
        {
            var premiumVariant = variantCode.GetVariantByCode() as IAmPremiumVariant;
            return premiumVariant.ConvertEpiQuantityToAxQuantity(epiQty);
        }

        public static decimal ParseAxQuantityToEpi(this IAmPremiumVariant premiumVariant, decimal axQuantity)
        {
            var tubeCoin = premiumVariant as CoinVariant;
            if (tubeCoin?.CoinTubeQuantity != null && tubeCoin.CoinTubeQuantity > 0)
            {
                return Math.Round(axQuantity / tubeCoin.CoinTubeQuantity ?? decimal.Zero, 3);
            }
            return axQuantity;
        }

        public static decimal ConvertAxWeightToExWeight(this IAmPremiumVariant premiumVariant, decimal unitMetalWeightOz)
        {
            var tubeCoin = premiumVariant as CoinVariant;
            if (tubeCoin?.CoinTubeQuantity != null && tubeCoin.CoinTubeQuantity > 0)
            {
                return (decimal)(unitMetalWeightOz * tubeCoin.CoinTubeQuantity);
            }
            return unitMetalWeightOz;
        }

        public static decimal ConvertEpiQuantityToAxQuantity(this IAmPremiumVariant premiumVariant, decimal epiQuantity)
        {
            var tubeCoin = premiumVariant as CoinVariant;
            if (tubeCoin?.CoinTubeQuantity != null && tubeCoin.CoinTubeQuantity > 0)
            {
                return (decimal)(tubeCoin.CoinTubeQuantity * epiQuantity);
            }
            return epiQuantity;
        }

        public static decimal ConvertQuantityToWeight(this IAmPremiumVariant premiumVariant, decimal epiQuantity)
        {
            if (premiumVariant == null) return decimal.Zero;
            return epiQuantity * premiumVariant.TroyOzWeight;
        }
        private static int? _stockStatusForSort = null;
        public static string StockStatus(this TrmVariant trmVariant)
        {
            return GetStockStatus(trmVariant);
        }

        public static int GetStatusOrderNumber(StockSummaryDto summary)
        {
            var status = summary.Status;

            switch (status)
            {
                case Enums.eStockStatus.InStock:
                    return 10;
                case Enums.eStockStatus.LowStock:
                case Enums.eStockStatus.SourcedToOrder:
                    return 20;
                case Enums.eStockStatus.AwaitingStock:
                    if (summary.BackorderAvailableQuantity > 0)
                    {
                        return 30;
                    }

                    return 40;
                case Enums.eStockStatus.PreOrder:
                case Enums.eStockStatus.SoldOut:
                    return 40;
                case Enums.eStockStatus.NoLongerAvailable:
                default:
                    return 50;
            }
        }

        public static int StockStatusForOrder(this TrmVariant trmVariant)
        {
            if (_stockStatusForSort != null)
            {
                return (int)_stockStatusForSort;
            }

            GetStockStatus(trmVariant);
            return _stockStatusForSort ?? 100;
        }

        private static string GetStockStatus(TrmVariant trmVariant)
        {
            var outOfStock = LocalizationService.GetStringByCulture(StringResources.OutOfStock, "Out of Stock", ContentLanguage.PreferredCulture);
            try
            {
                var inventoryHelper = ServiceLocator.Current.GetInstance<InventoryHelper>();
                var summary = inventoryHelper.GetStockSummary(trmVariant.ContentLink);
                var status = summary.Status;
                _stockStatusForSort = GetStatusOrderNumber(summary);
                if (status == Enums.eStockStatus.SoldOut || status == Enums.eStockStatus.NoLongerAvailable) return outOfStock;

                return Enums.eStockStatus.InStock.ToString();
            }
            catch
            { }

            return outOfStock;
        }

        public static bool IsCountryRestricted(this TrmVariant variant, string location)
        {
            return variant.RestrictedCountriesArray.Any(x => x.Equals(location, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}