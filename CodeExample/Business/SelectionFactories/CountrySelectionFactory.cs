using System.Collections.Generic;
using System.Linq;
using EPiServer.Shell.ObjectEditing;
using Mediachase.Commerce.Orders.Managers;

namespace TRM.Web.Business.SelectionFactories
{
    public class CountrySelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var countries = CountryManager.GetCountries().Country;

            return countries.Select(country => new SelectItem()
            {
                Text = country.Name, Value = country.Code
            });
        }
    }
}