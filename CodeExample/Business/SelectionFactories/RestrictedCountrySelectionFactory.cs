using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using TRM.Web.Helpers;
using TRM.Web.Models.Pages;

namespace TRM.Web.Business.SelectionFactories
{
    public class RestrictedCountrySelectionFactory : ISelectionFactory
    {
        private readonly IRestrictedCountriesHelper restrictedCountriesHelper;

        public RestrictedCountrySelectionFactory() : this(ServiceLocator.Current.GetInstance<IRestrictedCountriesHelper>())
        {
            
        }
        
        public RestrictedCountrySelectionFactory(IRestrictedCountriesHelper restrictedCountriesHelper)
        {
            this.restrictedCountriesHelper = restrictedCountriesHelper;
        }

        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            return restrictedCountriesHelper.GetRestrictedCountriesList().Select(country => new SelectItem()
            {
                Text = country.Name, Value = country.ThreeLettersIsoCode
            });
        }
    }
}