using System;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers.AutoInvest
{
    public interface IAutoPurchaseHelper
    {
        bool IsAutoInvestActivated();
        bool IsValidAutoInvestPageConfigureInStartPage();
        DateTime GetStartDate(DateTime currentDate);
        AutoInvestSettingsPage GetAutoInvestSettingsPage();
        EmailSettingsPage GetEmailSettingsPage();
        bool IsDisallowedDate(DateTime currentDate);
        bool IsStopTradingActivated();
        int GetBatchSize();
        int GetBatchDelay();
    }
}