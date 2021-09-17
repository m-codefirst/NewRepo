using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using TRM.Web.Models.DDS.BullionTax;
using TRM.Web.Models.Pages;
using TRM.Web.Models.Pages.Bullion;

namespace TRM.Web.Business.DataAccess
{
    public interface IBullionTaxRepository
    {
        IEnumerable<VatStatus> GetVatStatusList();
        IEnumerable<VatRate> GetVatRateList();
        IEnumerable<VatRule> GetVatRuleList();
    }

    [ServiceConfiguration(typeof(IBullionTaxRepository))]
    [ServiceConfiguration(typeof(BullionTaxRepository))]
    public class BullionTaxRepository : IBullionTaxRepository
    {
        private readonly IContentLoader _contentLoader;

        private Lazy<StartPage> StartPage
        {
            get
            {
                return new Lazy<StartPage>(() =>
                {
                    try
                    {
                        return _contentLoader.Get<StartPage>(ContentReference.StartPage);
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
        }

        private Lazy<TaxSettingPage> TaxSettingPage
        {
            get
            {
                return new Lazy<TaxSettingPage>(() =>
                {
                    if (StartPage.Value == null) return null;

                    try
                    {
                        var taxSettingPage = _contentLoader.Get<TaxSettingPage>(StartPage.Value.TaxSettingPage);
                        return taxSettingPage;
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
        }

        public BullionTaxRepository(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public IEnumerable<VatRate> GetVatRateList()
        {
            return TaxSettingPage.Value?.VatRates;
        }

        public IEnumerable<VatRule> GetVatRuleList()
        {
            return TaxSettingPage.Value?.VatRules;
        }

        public IEnumerable<VatStatus> GetVatStatusList()
        {
            return TaxSettingPage.Value?.VatStatus;
        }

    }
}