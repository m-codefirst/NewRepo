using System.Collections.Generic;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public interface IAmCountryHelper
    {
        List<Country> GetCountries();
        Country GetCountry(string code);
        List<Country> GetNonBlockedCountries();
    }
}