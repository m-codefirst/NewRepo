using System.Collections.Generic;
using Mediachase.Commerce.Customers;
using TRM.Shared.Models.DTOs;
using TRM.Web.Plugins.TestsHelperAdminPanel;

namespace TRM.Web.Services.TestAutomationHelper
{
    public interface ITestAutomationHelperService
    {
        TestsHelperAdminPanelUser CreateUniqueUser(bool bullionUser, string email, string password, string firstName, string lastName,
            string country, string currency, AccountKycStatus kycStatus);        
        
        bool UserExists(string email);

        TestsHelperAdminPanelUser AddBullionBalance(TestsHelperAdminPanelUser user, decimal balance);

        TestsHelperAdminPanelUser CreateOrUpdateEpiserverUser(string password, string email);

        void AddAdminRoles(string email);

        TestsHelperAdminPanelUser SetSippCustomer(TestsHelperAdminPanelUser user);

        TestsHelperAdminPanelUser AddStatements(TestsHelperAdminPanelUser user);

        TestsHelperAdminPanelUser AddInvoices(TestsHelperAdminPanelUser user);

        TestsHelperAdminPanelUser AddCredit(TestsHelperAdminPanelUser user, decimal credit);

        CustomerContact GetExistingContactByUserName(string name);
    }
}