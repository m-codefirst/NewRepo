using System.Collections.Generic;
using System.Web;
using TRM.Web.CustomProperties;

namespace TRM.Web.Helpers
{
    public interface IRestrictedCountriesHelper
    {
        IEnumerable<CountriesListItem> GetRestrictedCountriesList();

        string GetVisitorLocation(HttpRequestBase request);
    }
}