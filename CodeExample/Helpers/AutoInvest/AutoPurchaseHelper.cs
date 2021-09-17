using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers.AutoInvest
{
    public class AutoPurchaseHelper : IAutoPurchaseHelper
    {
        private readonly IContentLoader _contentLoader;

        public AutoPurchaseHelper(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public bool IsStopTradingActivated()
        {
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            return startPage.StopTrading;
        }

        public bool IsValidAutoInvestPageConfigureInStartPage()
        {
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            return startPage.LTSettingsPage != null;
        }

        public DateTime GetStartDate(DateTime currentDate)
        {
            var settingsPage = GetAutoInvestSettingsPage();

            var startDate = currentDate;
            //for (int i = 1; i < 1000; i++)
            for (int i = 1; i < 30; i++)
            {
                startDate = currentDate.AddDays(-i);
                if (settingsPage.DisAllowedDates.All(x => x.Date != startDate.Date))
                {
                    break;
                }
            }

            return startDate;
        }

        public bool IsDisallowedDate(DateTime currentDate)
        {
            var settingsPage = GetAutoInvestSettingsPage();
            if (settingsPage == null)
            {
                return true;
            }

            if (settingsPage.DisAllowedDates == null || !settingsPage.DisAllowedDates.Any())
            {
                return false;
            }

            if (settingsPage.DisAllowedDates.Any(x => x.Date == currentDate.Date))
            {
                return true;
            }

            return false;
        }

        public bool IsAutoInvestActivated()
        {
            var settingsPage = GetAutoInvestSettingsPage();
            if (settingsPage == null)
            {
                return false;
            }

            return settingsPage.EnableAutoInvest;
        }

        public int GetBatchSize()
        {
            var defaultValue = 10;
            var autoInvestBatchSize = GetAutoInvestSettingsPage()?.AutoInvestBatchSize ?? defaultValue;
            autoInvestBatchSize = autoInvestBatchSize > 0 ? autoInvestBatchSize : defaultValue;
            return autoInvestBatchSize;
        }

        public int GetBatchDelay()
        {
            return GetAutoInvestSettingsPage()?.AutoInvestBatchDelay ?? 1000;
        }

        public AutoInvestSettingsPage GetAutoInvestSettingsPage()
        {
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            var settingsPage = _contentLoader.Get<AutoInvestSettingsPage>(startPage.LTSettingsPage);
            return settingsPage;
        }  
        
        public EmailSettingsPage GetEmailSettingsPage()
        {
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            var settingsPage = _contentLoader.Get<EmailSettingsPage>(startPage.EmailSettingsPage);
            return settingsPage;
        }
    }
}