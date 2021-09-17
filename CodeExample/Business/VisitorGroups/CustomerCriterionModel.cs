using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Data.Dynamic;
using EPiServer.Personalization.VisitorGroups;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;

namespace TRM.Web.Business.VisitorGroups
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class CustomerCriterionModel : CriterionModelBase
    {
        public class CustomerFieldSelectionFactory : ISelectionFactory
        {
            IEnumerable<SelectListItem> ISelectionFactory.GetSelectListItems(Type property)
            {
                return GetAllPropertiesContact();
            }
        }

        [DojoWidget(
            WidgetType = "dijit/form/FilteringSelect",
            SelectionFactoryType = typeof(CustomerFieldSelectionFactory))]
        [Required]
        public string CustomerField { get; set; }

        public string Value { get; set; }

        private static List<SelectListItem> GetAllPropertiesContact()
        {
            var list = new List<SelectListItem>();

            var customerMetadata = DataContext.Current.MetaModel.MetaClasses
                .Cast<MetaClass>()
                .First(mc => mc.Name == Shared.Constants.StringConstants.CustomFields.ContactClassName);

            foreach (MetaField meta in customerMetadata.Fields)
            {
                list.Add(new SelectListItem
                {
                    Value = meta.Name,
                    Text = meta.Name
                });
            }
            return list;
        }

        public override ICriterionModel Copy()
        {
            return ShallowCopy();
        }
    }
}