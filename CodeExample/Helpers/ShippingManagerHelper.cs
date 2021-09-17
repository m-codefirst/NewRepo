using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Logging;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using TRM.Shared.Constants;
using TRM.Web.Models;

namespace TRM.Web.Helpers
{
    public class ShippingManagerHelper : IAmShippingManagerHelper
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(ShippingManagerHelper));


        public ShippingMethodSummary GetShippingMethodSummary(Guid id)
        {
            try
            {
                var shippingMethod = ShippingManager.GetShippingMethod(id);

                return ShippingMethodSummary(shippingMethod.ShippingMethod.FirstOrDefault());
            }
            catch
            {
                _logger.Log(Level.Warning, $"Shipping Method Id not valid - {id}");
                return null;
            }

        }

        public List<ShippingMethodSummary> GetShippingMethodsByMarket(string marketid, bool returnInactive)
        {
            var shippingMethods = ShippingManager.GetShippingMethodsByMarket(marketid, returnInactive);

            var results = new List<ShippingMethodSummary>();

            foreach (var method in shippingMethods.ShippingMethod)
            {
                var shippingMethod = ShippingMethodSummary(method);

                results.Add(shippingMethod);
            }

            return results;
        }

        private static ShippingMethodSummary ShippingMethodSummary(ShippingMethodDto.ShippingMethodRow method)
        {
            if (method == null)
            {
                return new ShippingMethodSummary();
            }

            var shippingMethod = new ShippingMethodSummary
            {
                Id = method.ShippingMethodId.ToString(),
                Name = method.Name,
                FriendlyName = method.DisplayName,
                DisplayMessage = method.DisplayName,
                DeliveryCost = new Money(method.BasePrice, method.Currency).ToString(),
                DeliveryCostDecimal = method.BasePrice,
                IsActive = method.IsActive,
                IsDeffault = method.IsDefault,
                SortOrder = method.Ordering,
                Currency = method.Currency
            };

            var shippingMethodParameterRow = method.GetShippingMethodParameterRows()
                .FirstOrDefault(x => x.Parameter == "CutOffTime");
            if (shippingMethodParameterRow != null)
            {
                shippingMethod.CutOffTime = shippingMethodParameterRow.Value ?? string.Empty;
            }

            //days of the week
            for (var i = 0; i < 7; i++)
            {
                var dayOfweek = (DayOfWeek)i;
                var availableDayParameter = method.GetShippingMethodParameterRows()
                    .FirstOrDefault(x => x.Parameter == "Available" + dayOfweek);
                if (availableDayParameter == null) continue;
                shippingMethod.AvailableDays.Add(dayOfweek);
            }

            shippingMethodParameterRow = method.GetShippingMethodParameterRows()
                .FirstOrDefault(x => x.Parameter == "AllItemsInStock");
            if (shippingMethodParameterRow != null)
            {
                bool allInStock;
                bool.TryParse(shippingMethodParameterRow.Value, out allInStock);
                shippingMethod.AllProductsInStock = allInStock;
            }

            //Min
            shippingMethodParameterRow = method.GetShippingMethodParameterRows()
                .FirstOrDefault(x => x.Parameter == "MinOrderAmount");
            if (shippingMethodParameterRow != null)
            {
                decimal minOrderValue;
                decimal.TryParse(shippingMethodParameterRow.Value, out minOrderValue);
                shippingMethod.MinOrderValue = minOrderValue;
            }

            //Max
            shippingMethodParameterRow = method.GetShippingMethodParameterRows()
                .FirstOrDefault(x => x.Parameter == "MaxOrderAmount");
            if (shippingMethodParameterRow != null)
            {
                decimal maxOrderValue;
                decimal.TryParse(shippingMethodParameterRow.Value, out maxOrderValue);
                shippingMethod.MaxOrderValue = maxOrderValue;
            }

            //Min Weight
            shippingMethodParameterRow = method.GetShippingMethodParameterRows()
                .FirstOrDefault(x => x.Parameter == StringConstants.ShippingFields.MinWeight);
            if (shippingMethodParameterRow != null)
            {
                decimal value;
                decimal.TryParse(shippingMethodParameterRow.Value, out value);
                shippingMethod.MinWeight = value;
            }

            //Max Weight
            shippingMethodParameterRow = method.GetShippingMethodParameterRows()
                .FirstOrDefault(x => x.Parameter == StringConstants.ShippingFields.MaxWeight);
            if (shippingMethodParameterRow != null)
            {
                decimal value;
                decimal.TryParse(shippingMethodParameterRow.Value, out value);
                shippingMethod.MaxWeight = value;
            }

            shippingMethodParameterRow = method.GetShippingMethodParameterRows()
                .FirstOrDefault(x => x.Parameter == "ObsMethodName");
            if (shippingMethodParameterRow != null)
            {
                shippingMethod.ObsMethodName = shippingMethodParameterRow.Value ?? string.Empty;
            }
            shippingMethodParameterRow = method.GetShippingMethodParameterRows()
              .FirstOrDefault(x => x.Parameter == StringConstants.ShippingFields.ForGiftingOnly);
            if (shippingMethodParameterRow != null)
            {
                shippingMethod.ForGiftingOnly = Convert.ToBoolean(shippingMethodParameterRow.Value ?? bool.FalseString);
            }

            //Countries
            var countryRestrictions = method.GetShippingCountryRows();

            if (countryRestrictions.Any())
            {
                foreach (var country in countryRestrictions)
                {
                    shippingMethod.UnavailableCountries.Add(country.CountryId);
                }
            }
            return shippingMethod;
        }
    }
}