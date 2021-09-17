using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using EPiServer.Personalization.VisitorGroups;
using TRM.Shared.Constants;

namespace TRM.Web.Business.VisitorGroups
{
    public class CustomerTypeModel : CriterionModelBase
    {
        public class CustomerTypeSelectionFactory : ISelectionFactory
        {
            IEnumerable<SelectListItem> ISelectionFactory.GetSelectListItems(Type property)
            {
                return new List<SelectListItem>
                {
                    new SelectListItem
                    {
                        Text = String.Empty,
                        Value = String.Empty
                    },
                    new SelectListItem
                    {
                        Text = StringConstants.CustomerType.Consumer,
                        Value = StringConstants.CustomerType.Consumer
                    },
                    new SelectListItem
                    {
                        Text = StringConstants.CustomerType.Bullion,
                        Value = StringConstants.CustomerType.Bullion
                    },
                    new SelectListItem
                    {
                        Text = StringConstants.CustomerType.ConsumerAndBullion,
                        Value = StringConstants.CustomerType.ConsumerAndBullion
                    }
                };
            }
        }

        [DojoWidget(
            WidgetType = "dijit/form/FilteringSelect",
            SelectionFactoryType = typeof(CustomerTypeSelectionFactory))]
        [Required]
        public string IsCustomerType { get; set; }

        [DojoWidget(
            WidgetType = "dijit/form/FilteringSelect",
            SelectionFactoryType = typeof(CustomerTypeSelectionFactory))]
        public string IsNotCustomerType { get; set; }

        public override ICriterionModel Copy()
        {
            return ShallowCopy();
        }
    }

}