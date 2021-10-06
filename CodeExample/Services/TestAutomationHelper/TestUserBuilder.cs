using EPiServer.ServiceLocation;
using TRM.Shared.Models.DTOs;
using TRM.Web.Plugins.TestsHelperAdminPanel;

namespace TRM.Web.Services.TestAutomationHelper
{
    public class TestUserBuilder
    {
        private readonly ITestAutomationHelperService _testAutomationHelperService;

        private TestsHelperAdminPanelUser _user;

        public TestUserBuilder(ITestAutomationHelperService testAutomationHelperService)
        {
            _testAutomationHelperService = testAutomationHelperService;
        }

        public static TestUserBuilder Instance()
        {
            return ServiceLocator.Current.GetInstance<TestUserBuilder>();
        }

        public TestsHelperAdminPanelUser Build()
        {
            return _user;
        }

        public TestUserBuilder InitializeUnique(bool bullionUser, string email, string password, string firstName, string lastName,
            string country, string currency, AccountKycStatus kycStatus)
        {
            this._user = _testAutomationHelperService.CreateUniqueUser(bullionUser, email, password, firstName, lastName, country, currency, kycStatus);

            return this;
        }

        public TestUserBuilder Initialize(bool bullionUser, string email, string password, string firstName, string lastName,
            string country, string currency, AccountKycStatus kycStatus = AccountKycStatus.Approved)
        {
            if (_testAutomationHelperService.UserExists(email))
            {
                _testAutomationHelperService.CreateOrUpdateEpiserverUser(password, email);
            
                var customer = _testAutomationHelperService.GetExistingContactByUserName(email);
            
                this._user = new TestsHelperAdminPanelUser(email, true)
                { Message = "User exists. Password updated.", CustomerContact = customer };
                return this;
            }

            return InitializeUnique(bullionUser, email, password, firstName, lastName, country, currency, kycStatus);
        }

        public TestUserBuilder WithBalance(decimal balance)
        {
            if (!this._user.Success)
            {
                return this;
            }

            this._user = this._testAutomationHelperService.AddBullionBalance(this._user, balance);
            return this;
        }

        public TestUserBuilder WithAdminAccess()
        {
            if (!this._user.Success)
            {
                return this;
            }

            _testAutomationHelperService.AddAdminRoles(this._user.Name);
            return this;
        }

        public TestUserBuilder SippCustomer()
        {
            if (!this._user.Success)
            {
                return this;
            }

            this._user = this._testAutomationHelperService.SetSippCustomer(this._user);
            return this;
        }

        public TestUserBuilder WithStatements()
        {
            if (!this._user.Success)
            {
                return this;
            }

            this._user = this._testAutomationHelperService.AddStatements(this._user);
            return this;
        }
        public TestUserBuilder WithInvoices()
        {
            if (!this._user.Success)
            {
                return this;
            }

            this._user = this._testAutomationHelperService.AddInvoices(this._user);
            return this;
        }

        public TestUserBuilder WithCredit(decimal credit)
        {
            if (!this._user.Success)
            {
                return this;
            }

            this._user = this._testAutomationHelperService.AddCredit(this._user, credit);
            return this;
        }
    }
}