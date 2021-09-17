using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using TRM.Web.Business.DataAccess;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public class CountryHelper : IAmCountryHelper
    {
        protected Lazy<CountryRepository> CountryRepository = new Lazy<CountryRepository>(() => ServiceLocator.Current.GetInstance<CountryRepository>());

        public virtual Country GetCountry(string code)
        {
            return CountryRepository.Value.Find(c => c.CountryCode == code).Select(UpdateCountryZone).FirstOrDefault();
        }

        public virtual List<Country> GetCountries()
        {
            var countries = CountryRepository.Value.FindAll()
                           .OrderBy(a => a.CountryName).ToList();

            if (countries.Any()) return countries.Select(UpdateCountryZone).ToList();

            countries.Add(new Country { CountryName = "Great Britain", Zone = CountryZone.UK, CountryCode = "GBR", AvailableCurrencyCodes = "GBP", Id = Guid.NewGuid() });
            countries.Add(new Country { CountryName = "Hong Kong", CountryCode = "HKG", Zone = CountryZone.ROW, AvailableCurrencyCodes = "HKD", Id = Guid.NewGuid() });
            return countries;
        }

        public virtual List<Country> GetNonBlockedCountries()
        {
            var countries = CountryRepository.Value.Find(x => !x.ExcludeThisCountryFromRegistration)
                .OrderBy(a => a.CountryName).ToList();

            if (countries.Any()) return countries.Select(UpdateCountryZone).ToList();

            countries.Add(new Country { CountryName = "Great Britain", Zone = CountryZone.UK, CountryCode = "GBR", AvailableCurrencyCodes = "GBP", Id = Guid.NewGuid() });
            countries.Add(new Country { CountryName = "Hong Kong", CountryCode = "HKG", Zone = CountryZone.ROW, AvailableCurrencyCodes = "HKD", Id = Guid.NewGuid() });
            return countries;
        }

        private Country UpdateCountryZone(Country country)
        {
            if (country.Zone == CountryZone.ROW || 
                country.Zone == CountryZone.EUROPE || 
                country.Zone == CountryZone.UK)
            {
                return country;
            }

            country.Zone = CountryZone.ROW;
            return country;
        }
    }
}