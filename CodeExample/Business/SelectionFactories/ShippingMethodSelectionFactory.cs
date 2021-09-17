using System.Collections.Generic;
using System.Linq;
using EPiServer.Shell.ObjectEditing;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

namespace TRM.Web.Business.SelectionFactories
{
    public class ShippingMethodSelectionFactory : ISelectionFactory
    {
        public ShippingMethodSelectionFactory() { }

        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var methods = ShippingManager.GetShippingMethods(SiteContext.Current.LanguageName);

            var shippingRows = methods.ShippingMethod.Rows.Cast<ShippingMethodDto.ShippingMethodRow>().ToList();

            return shippingRows.Select(shippingRow => new SelectItem()
            {
                Text = shippingRow.DisplayName, Value = shippingRow.Name
            }).Cast<ISelectItem>().ToList();
        }
    }
}