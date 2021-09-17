using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using TRM.Web.Helpers;

namespace TRM.Web.Business.SelectionFactories
{
    public class CurrencySelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var currencyHelper = ServiceLocator.Current.GetInstance<IAmCurrencyHelper>();
            var currencies = currencyHelper.GetCurrencies();
            return currencies.Select(currency => new SelectItem
            {
                Text = currency.Value,
                Value = currency.Key
            });
        }
    }


    // Temp: Will move to a separate class later
    public class MetalSelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            return new List<SelectItem>
            {
                new SelectItem(){ Text="Gold", Value="xau"},
                new SelectItem(){ Text="Silver", Value="xag"},
                new SelectItem(){ Text="Platinum", Value="xpt"}
            };
        }
    }
}