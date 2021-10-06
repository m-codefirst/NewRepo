using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Security;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Shared.Models;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Plugins.TestsHelperAdminPanel;
using TRM.Web.Services.InvoiceStatements;

namespace TRM.Web.Services.TestAutomationHelper
{
    public class TestAutomationHelperService : ITestAutomationHelperService
    {
        private readonly IAddressBookService _addressBookService;
        private readonly IBullionUserService _bullionUserService;
        private readonly IUserService _userService;
        private readonly CustomerContext _customerContext;
        private readonly IBullionStatementService _bullionStatementService;
        private readonly IBullionInvoiceService _bullionInvoiceService;
        private readonly RandomNumberGenerator _randomNumberGenerator;

        public TestAutomationHelperService(IAddressBookService addressBookService, IBullionUserService bullionUserService,
            CustomerContext customerContext, IBullionStatementService bullionStatementService, IBullionInvoiceService bullionInvoiceService, IUserService userService)
        {
            _addressBookService = addressBookService;
            _bullionUserService = bullionUserService;
            _customerContext = customerContext;
            _bullionStatementService = bullionStatementService;
            _bullionInvoiceService = bullionInvoiceService;
            _userService = userService;
            _randomNumberGenerator = RandomNumberGenerator.Create();
        }

        public bool UserExists(string email)
        {
            return Membership.GetUser(email) != null;
        }

        public TestsHelperAdminPanelUser CreateUniqueUser(bool bullionUser, string email, string password, string firstName, string lastName,
            string country, string currency, AccountKycStatus kycStatus = AccountKycStatus.Approved)
        {
            string uniqueEmail = email;
            var splitted = email.Split('@');

            for (var i = 0; i < 1000; i++)
            {
                if (Membership.GetUser(uniqueEmail) != null)
                {
                    uniqueEmail = $"{splitted[0]}_{i}@{splitted[1]}";
                }
                else
                {
                    break;
                }
            }

            return CreateUser(bullionUser, uniqueEmail, password, firstName, lastName, country, currency, kycStatus);
        }

        private TestsHelperAdminPanelUser CreateUser(bool bullionUser, string email, string password, string firstName, string lastName,
            string country, string currency, AccountKycStatus kycStatus = AccountKycStatus.Approved)
        {
            try
            {
                var address = new AddressModel
                {
                    Name = StringConstants.PreciousMetalsRegisteredAddressName,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Line1 = "London",
                    Line2 = "London",
                    City = "London",
                    PostalCode = "12-124",
                    CountryCode = country,
                    CountryRegion = new CountryRegionModel
                    {
                        Region = "London"
                    },
                    BillingDefault = true,
                    ShippingDefault = true
                };

                var customerAddress = CustomerAddress.CreateInstance();
                customerAddress[StringConstants.AddressFieldNames.County] = "AAA";
                customerAddress[StringConstants.CustomFields.BullionKycAddress] = true;
                _addressBookService.MapToAddress(address, customerAddress);

                var obsNumber = TestObsNumbersRepository.GetNumber();
                var userModel = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Password = password,
                    ByPost = false,
                    ByTelephone = false,
                    ByEmail = false,
                    Title = "Mr",
                    FirstName = firstName,
                    LastName = lastName,
                    MiddleName = firstName,
                    SecondSurname = lastName,
                    BirthDate = DateTime.Now.AddYears(-20),
                    Gender = "male",
                    FullName = $"{firstName} {lastName}",
                    PhoneNumber = "123456789",
                    EmailConsentSource = Constants.StringConstants.CustomerContactSourcePreference.StandAloneRegistation,
                    EmailConsentDateTime = DateTime.Now,
                    PostConsentSource = Constants.StringConstants.CustomerContactSourcePreference.StandAloneRegistation,
                    PostConsentDateTime = DateTime.Now,
                    TelephoneConsentSource = Constants.StringConstants.CustomerContactSourcePreference.StandAloneRegistation,
                    TelephoneConsentDateTime = DateTime.Now,
                    Addresses = new List<CustomerAddress>(new[] { customerAddress }),
                    MobilePhone = "123456789",
                    Currency = currency,

                    TwoStepAuthenticationQuestion = "1",
                    TwoStepAuthenticationAnswer = "abc",
                    RegistrationSource = "Automated Tests",

                    CustomerType = bullionUser ? StringConstants.CustomerType.Bullion : StringConstants.CustomerType.Consumer,
                    BullionCustomerType = ((int)Enums.BullionCustomerType.Standard).ToString(),
                    BullionPremiumGroupInt = 0,
                    CustomerKycData = new KycQueryResultDto
                    {
                        Id3Response = string.Empty,
                        Status = kycStatus,
                    },

                    ObsAccountNumber = obsNumber.OBSAccountNumber,
                    BullionAccountNumber = bullionUser ? obsNumber.CustomerBullionObsAccountNumber : null,
                    IsActivated = true
                };

                var registration = bullionUser
                    ? _bullionUserService.BullionRegisterAccount(userModel)
                    : _userService.RegisterAccount(userModel);

                if (registration.Result.Status != Enums.eCustomerStatus.Created &&
                    registration.Result.Status != Enums.eCustomerStatus.CreatedWithWarning)
                {
                    return new TestsHelperAdminPanelUser(email, false)
                    {
                        Message =
                            $"Cannot register for {email}. Status: {registration.Result.Status}, messages: {string.Join(",", registration.Result.Messages)}"
                    };
                }

                return new TestsHelperAdminPanelUser(email, true)
                { CustomerContact = registration.Contact };
            }
            catch (Exception e)
            {
                return new TestsHelperAdminPanelUser(email, false) { Message = e.Message };
            }
        }

        public void AddAdminRoles(string email)
        {
            const string AdminsGroupName = "Administrators";
            AddRolesIfMissing(email, new[] { AdminsGroupName });
        }

        private static void AddRolesIfMissing(string adminUser, string[] roles)
        {
            var currentRoles = Roles.GetRolesForUser(adminUser);
            var rolesToAdd = roles.Where(r => !currentRoles.Contains(r)).ToList();

            if (rolesToAdd.Any())
            {
                Roles.AddUserToRoles(adminUser, rolesToAdd.ToArray());
            }
        }

        public TestsHelperAdminPanelUser CreateOrUpdateEpiserverUser(string password, string email)
        {
            try
            {
                var user = Membership.GetUser(email);

                if (user == null)
                {
                    Membership.CreateUser(email, password, email,
                        null, null, true, out var createStatus);

                    if (createStatus != MembershipCreateStatus.Success)
                    {
                        return new TestsHelperAdminPanelUser(email, false) { Message = $"Cannot create user: {createStatus}" };
                    }
                }
                else
                {
                    user.UnlockUser();
                    var resetedPass = user.ResetPassword();
                    user.ChangePassword(resetedPass, password);
                }
            }
            catch (Exception e)
            {
                return new TestsHelperAdminPanelUser(email, false) { Message = $"{nameof(CreateOrUpdateEpiserverUser)}_{e.Message}" };
            }

            return new TestsHelperAdminPanelUser(email, true);
        }

        public TestsHelperAdminPanelUser AddBullionBalance(TestsHelperAdminPanelUser user, decimal balance)
        {
            try
            {
                _bullionUserService.UpdateBullionCustomerBalances(
                    new AxImportData.CustomerBalance
                    {
                        Balances = new AxImportData.Balances
                        {
                            AvailableToSpend = balance,
                            AvailableToWithdraw = balance,
                            Effective = balance
                        }
                    }, user.UserGuid);
            }
            catch (Exception e)
            {
                return new TestsHelperAdminPanelUser(user.Name, false) { Message = $"{nameof(AddBullionBalance)}_{e.Message}" };
            }

            return user;
        }


        public TestsHelperAdminPanelUser SetSippCustomer(TestsHelperAdminPanelUser user)
        {
            try
            {
                var customer = _customerContext.GetContactById(user.UserGuid);
                customer[StringConstants.CustomFields.BullionCustomerType] = (int)Enums.BullionCustomerType.SIPPCustomer;
                customer.SaveChanges();
            }
            catch (Exception e)
            {
                return new TestsHelperAdminPanelUser(user.Name, false) { Message = $"{nameof(SetSippCustomer)}_{e.Message}" };
            }

            return user;
        }

        public TestsHelperAdminPanelUser AddCredit(TestsHelperAdminPanelUser user, decimal credit)
        {
            try
            {
                var customer = _customerContext.GetContactById(user.UserGuid);
                customer[StringConstants.CustomFields.CreditLimitFieldName] = credit;
                customer[StringConstants.CustomFields.CreditUsedEPiFieldName] = (decimal)1;
                customer[StringConstants.CustomFields.CreditUsedFieldName] = (decimal)1;

                customer.SaveChanges();
            }
            catch (Exception e)
            {
                return new TestsHelperAdminPanelUser(user.Name, false) { Message = $"{nameof(SetSippCustomer)}_{e.Message}" };
            }

            return user;
        }

        public TestsHelperAdminPanelUser AddStatements(TestsHelperAdminPanelUser user)
        {
            try
            {
                var statement = new AxImportData.CustomerStmtsStatement
                {
                    OpeningBalance = "20000",
                    ClosingBalance = "10000",
                    CustomerCode = "1234",
                    FromDate = "2021-02-01",
                    ToDate = "2021-03-01",
                    Generated = "2021-03-01",
                    Items = new[]
                    {
                        new AxImportData.CustomerStmtsStatementStoredItem
                        {
                            Status = "Settled",
                            OrderId = "OB-7370949",
                            Qty = "48.584",
                            EpiTransId = Guid.NewGuid().ToString().ToUpper(),
                            ItemId = Guid.NewGuid().ToString().ToUpper()
                        },
                        new AxImportData.CustomerStmtsStatementStoredItem
                        {
                            Status = "Settled",
                            OrderId = "OB-1234567",
                            Qty = "4.584",
                            EpiTransId = Guid.NewGuid().ToString().ToUpper(),
                            ItemId = Guid.NewGuid().ToString().ToUpper()
                        }
                    },
                    Transactions = new[]
                    {
                        new AxImportData.CustomerStmtsStatementTransaction
                        {
                            EpiTransId = Guid.NewGuid().ToString().ToUpper(),
                            TransDate = "2020-07-27",
                            Type = "DeliverFromVault",
                            Description = "Manual funds withdrawal",
                            VariantCode = "RMRCSKB",
                            Status = "Pending",
                            Amount = "0.000",
                            SubmittedBy = "Customer",
                            BankAccount = "",
                        }
                    }
                };

                _bullionStatementService.AddCustomerStatementFromImportData(statement, user.UserGuid);
            }
            catch (Exception e)
            {
                return new TestsHelperAdminPanelUser(user.Name, false) { Message = $"{nameof(AddStatements)}_{e.Message}" };
            }

            return user;
        }

        public TestsHelperAdminPanelUser AddInvoices(TestsHelperAdminPanelUser user)
        {
            try
            {
                var invoicesInvoice = new AxImportData.InvoicesInvoice
                {
                    CustomerCode = user.CustomerContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber),
                    InvoiceDate = "2020-06-30",
                    InvoicePeriodFrom = "2020-04-27",
                    InvoicePeriodTo = "2020-06-30",
                    InvoiceNumber = "Fee - AU1, 27/04/2020-30/06/2020 ref: OB-0C65751",
                    TotalAmount = "5.060",
                    TotalVAT = "1.010",
                    Lines = new[]
                    {
                        new AxImportData.InvoicesInvoiceLine
                        {
                            DateFrom = "2020-04-27",
                            DateTo = "2020-06-30",
                            Amount = "5.060",
                            VAT = "1.010",
                            Description = "Some description",
                            ProductId = "AU1"
                        }
                    }
                };

                _bullionInvoiceService.AddCustomerInvoiceFromImportData(invoicesInvoice, user.UserGuid);
            }
            catch (Exception e)
            {
                return new TestsHelperAdminPanelUser(user.Name, false) { Message = $"{nameof(AddInvoices)}_{e.Message}" };
            }

            return user;
        }

        public CustomerContact GetExistingContactByUserName(string name)
        {
            return _userService.GetExistingContactByUserName(name);
        }
    }
}