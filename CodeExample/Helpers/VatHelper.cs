using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using Mediachase.Commerce.Customers;
using TRM.Shared.Helpers;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers
{
    public class VatHelper : IAmVatHelper
    {
        private readonly CustomerContext _customerContext;
        private readonly IContentLoader _contentLoader;
        private readonly IAmContactAuditHelper _contactAuditHelper;

        public VatHelper(CustomerContext customerContext, IContentLoader contentLoader, IAmContactAuditHelper contactAuditHelper)
        {
            _customerContext = customerContext;
            _contentLoader = contentLoader;
            _contactAuditHelper= contactAuditHelper;
        }

        public void UpdateCustomerGroup(string countryCode)
        {
            var customer = _customerContext.CurrentContact;

            if (customer?.PrimaryKeyId == null) return;

            UpdateCustomerGroup(countryCode, customer);
        }

        public bool IsNoneVatPricedDeliveryCountry(string countryCode)
        {
            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;
            if (string.IsNullOrWhiteSpace(startPage.CountriesWithVat))
            {
                return false;
            }

            return !startPage.CountriesWithVat.Contains(countryCode);
        }

        public void UpdateCustomerGroup(string countryCode, CustomerContact customer)
        {
            if (customer == null) return;

            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;
            if (startPage == null) return;

            var currentGroup = customer.CustomerGroup;

            if (string.IsNullOrWhiteSpace(startPage.CountriesWithVat))
            {
                customer.CustomerGroup = string.Empty;
            }
            else
            {
                customer.CustomerGroup = startPage.CountriesWithVat.Contains(countryCode) ? string.Empty : Shared.Constants.StringConstants.CustomGroups.NoVat;
            }

            if (currentGroup == customer.CustomerGroup) return;

            _contactAuditHelper.WriteCustomerAudit(customer, "Group Changed", string.Format("From {0} to {1}", currentGroup, customer.CustomerGroup));

            customer.SaveChanges();
        }
    }
}