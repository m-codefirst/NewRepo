using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Find.Commerce;
using EPiServer.Find.Helpers;
using Mediachase.Commerce.Customers;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.Pages;
using TRM.Web.Services.Portfolio;

namespace TRM.Web.Services.AutoInvest
{
    public class AutoInvestProductsService : IAutoInvestProductsService
    {
        private readonly IContentLoader _contentLoader;
        private readonly IAutoInvestUserService _userService;
        private readonly IBullionPortfolioService _bullionPortfolioService;

        public AutoInvestProductsService(IContentLoader contentLoader, IAutoInvestUserService userService, IBullionPortfolioService bullionPortfolioService)
        {
            _contentLoader = contentLoader;
            _userService = userService;
            _bullionPortfolioService = bullionPortfolioService;
        }

        public List<AutoInvestProductDto> GetProducts(CustomerContact contact)
        {
            var result = GetProductsFromSettingsPage();
            ApplyUserAutoInvestData(contact, result);
            ApplyUserInvestmentsStatusData(result);

            return result;
        }

        public List<int> GetAvailableInvestmentDates()
        {
            var settingsPage = GetAutoInvestSettingsPage();

            var start = settingsPage.InvestmentDayMinValue < 1 ? 1 : settingsPage.InvestmentDayMinValue;
            var end = settingsPage.InvestmentDayMaxValue < start ? start : settingsPage.InvestmentDayMaxValue;

            var availableInvestmentDates =
                Enumerable.Range(start, end - start + 1).ToList();
            return availableInvestmentDates;
        }

        private void ApplyUserInvestmentsStatusData(List<AutoInvestProductDto> result)
        {
            var portfolioVariantItems = _bullionPortfolioService.GetPortfolioVariantItems().ToList();

            foreach (var autoInvestProductDto in result)
            {
                var portfolioVariant = portfolioVariantItems.FirstOrDefault(x =>
                    x.VariantCode.Equals(autoInvestProductDto.Code, StringComparison.InvariantCultureIgnoreCase));

                autoInvestProductDto.CurrentInvestment = portfolioVariant?.CurrentPriceValue ?? 0;
            }
        }

        private void ApplyUserAutoInvestData(CustomerContact contact, List<AutoInvestProductDto> result)
        {
            var productsWithAutoInvest = _userService.GetInvestmentOptions(contact);
            foreach (var autoInvestProductDto in result)
            {
                if (productsWithAutoInvest.ContainsKey(autoInvestProductDto.Code))
                {
                    var monthlyAmount = productsWithAutoInvest[autoInvestProductDto.Code];

                    autoInvestProductDto.MonthlyInvestment = monthlyAmount;
                    autoInvestProductDto.Active = monthlyAmount > 0;
                }
                else
                {
                    autoInvestProductDto.MonthlyInvestment = 0;
                    autoInvestProductDto.Active = false;
                }
            }
        }

        private List<AutoInvestProductDto> GetProductsFromSettingsPage()
        {
            var settingsPage = GetAutoInvestSettingsPage();
            if (settingsPage?.AutoInvestProducts == null)
            {
                return Enumerable.Empty<AutoInvestProductDto>().ToList();
            }

            return settingsPage.AutoInvestProducts
                .Select(x =>
                {
                    if (!_contentLoader.TryGet(x.ProductReference, out VirtualVariantBase product))
                    {
                        return null;
                    }

                    var minInvestmentAmounts = product.MinSpendConfigs?
                        .Select(trmMoney => new {Currency = trmMoney.Currency.Trim().ToLowerInvariant(), trmMoney.Amount})
                        .DistinctBy(x1 => x1.Currency)
                        .ToDictionary(x2 => x2.Currency, x3 => x3.Amount);

                    return new AutoInvestProductDto
                    {
                        Name = product.Name, Code = product.Code, ImageUrl = product.DefaultImageUrl(),
                        ProductReference = x.ProductReference,
                        MinInvestmentAmount = minInvestmentAmounts
                    };
                })
                .Where(x => x != null)
                .ToList();
        }

        private AutoInvestSettingsPage GetAutoInvestSettingsPage()
        {
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            var settingsPage = _contentLoader.Get<AutoInvestSettingsPage>(startPage.LTSettingsPage);
            return settingsPage;
        }
    }

    public class AutoInvestProductDto
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public Dictionary<string, decimal> MinInvestmentAmount { get; set; }
        public bool Active { get; set; }
        public decimal MonthlyInvestment { get; set; }
        public decimal CurrentInvestment { get; set; }
        public ContentReference ProductReference { get; set; }
        public string ImageUrl { get; set; }
    }
}