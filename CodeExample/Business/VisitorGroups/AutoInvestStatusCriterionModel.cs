using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Data.Dynamic;
using EPiServer.Personalization.VisitorGroups;

namespace TRM.Web.Business.VisitorGroups
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class AutoInvestStatusCriterionModel : CriterionModelBase
    {
        public class AutoInvestStatusSelectionFactory : ISelectionFactory
        {
            IEnumerable<SelectListItem> ISelectionFactory.GetSelectListItems(Type property)
            {
                var selectListItems = Extentions.EnumExtensions.ToSelectListItems<Constants.Enums.AutoInvestUpdateOrderStatus>().ToList();
                return selectListItems;
            }
        }

        [DojoWidget(
            WidgetType = "dijit/form/FilteringSelect",
            SelectionFactoryType = typeof(AutoInvestStatusSelectionFactory))]
        [Required]
        public string CustomerAutoInvestStatus { get; set; }

        public override ICriterionModel Copy()
        {
            return ShallowCopy();
        }
    }
}