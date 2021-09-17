using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer;
using EPiServer.Core;
using TRM.Web.CustomProperties;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers
{
    public class RestrictedCountriesHelper : IRestrictedCountriesHelper
    {
        private readonly IContentLoader contentLoader;

        public RestrictedCountriesHelper(IContentLoader contentLoader)
        {
            this.contentLoader = contentLoader;
        }

        public IEnumerable<CountriesListItem> GetRestrictedCountriesList()
        {
            contentLoader.TryGet<StartPage>(ContentReference.StartPage, out var startPage);
            if (startPage == null || startPage.CatalogSettingsPage == null)
            {
                return Enumerable.Empty<CountriesListItem>();
            }

            contentLoader.TryGet<CatalogSettingsPage>(startPage.CatalogSettingsPage, out var catalogSettingsPage);
            if (catalogSettingsPage?.RestrictedCountriesList == null)
            {
                return Enumerable.Empty<CountriesListItem>();
            }

            return catalogSettingsPage.RestrictedCountriesList.ToList();
        }

        public string GetVisitorLocation(HttpRequestBase request)
        {
            var twoLettersCountryCode = request.Headers["CF-IPCountry"] ?? string.Empty;
            //Uncomment to test it locally
            // if (request.QueryString.AllKeys.Any(x=>x.Equals("location", StringComparison.InvariantCultureIgnoreCase)))
            // {
            //     twoLettersCountryCode = request.QueryString["location"];
            // }
            
            var threeLetterCountryCode = this.ConvertTo3LetterIsoCountryCode(twoLettersCountryCode);

            return threeLetterCountryCode;
        }

        public string ConvertTo3LetterIsoCountryCode(string twoLetterIsoCountryCode)
        {
            return GetRestrictedCountriesList().FirstOrDefault(x =>
                    x.TwoLetterIsoCode.Equals(twoLetterIsoCountryCode, StringComparison.InvariantCultureIgnoreCase))
                ?.ThreeLettersIsoCode ?? string.Empty;
        }
    }
}