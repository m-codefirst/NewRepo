using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

namespace Hephaestus.Commerce.Shared.Facades
{
    [ServiceConfiguration(typeof(CountryManagerFacade), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CountryManagerFacade
    {
        public virtual CountryDto GetCountries()
        {
            return CountryManager.GetCountries();
        }

        public virtual CountryDto.CountryRow GetCountryByCountryCode(string countryCode)
        {
            CountryDto dataset = CountryManager.GetCountry(countryCode, false);
            CountryDto.CountryDataTable table = dataset.Country;

            return (table.Rows.Count == 1) ? table.Rows[0] as CountryDto.CountryRow : null;
        }
    }
}
