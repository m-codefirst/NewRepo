using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Shared.Models;
using Mediachase.Commerce.Customers;
using TRM.IntegrationServices.Constants;
using TRM.IntegrationServices.Models;
using TRM.IntegrationServices.Models.Export.Payloads.BankAccount;
using TRM.IntegrationServices.Models.Import;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Web.Business.DataAccess;
using TRM.Web.Helpers;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Services.CustomersExportTransaction;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Services.Import
{
    public class BullionGbiDataImportService : BaseXmlImportService, IBullionGbiDataImportService
    {
        private readonly ICustomerBankAccountHelper _customerBankAccountHelper;
        private readonly ICustomerContactService _customerContactService;
        private readonly IUserService _userService;
        private readonly ICustomersExportTransactionService _customersExportTransactionService;
        private readonly IAddressBookService _addressBookService;
        private readonly IBullionDocumentRepository _bullionDocumentRepository;
        private readonly IExportTransactionService _exportTransactionService;
        private readonly IAmSecurityQuestionHelper _securityQuestionHelper;
        private readonly IBullionPremiumGroupHelper _bullionPremiumGroupHelper;

        public BullionGbiDataImportService(
            ICustomerBankAccountHelper customerBankAccountHelper,
            ICustomerContactService customerContactService,
            IExportTransactionService exportTransactionService,
            IUserService userService,
            IAddressBookService addressBookService,
            ICustomersExportTransactionService customersExportTransactionService,
            IBullionDocumentRepository bullionDocumentRepository,
            IAmSecurityQuestionHelper securityQuestionHelper, IBullionPremiumGroupHelper bullionPremiumGroupHelper)
        {
            _customerBankAccountHelper = customerBankAccountHelper;
            _customerContactService = customerContactService;
            _exportTransactionService = exportTransactionService;
            _userService = userService;
            _addressBookService = addressBookService;
            _customersExportTransactionService = customersExportTransactionService;
            _bullionDocumentRepository = bullionDocumentRepository;
            _securityQuestionHelper = securityQuestionHelper;
            _bullionPremiumGroupHelper = bullionPremiumGroupHelper;
        }

        public TrmResponse ImportBullionGbiData(ImportXmlRequest importXmlRequest)
        {
            var response = new TrmResponse();
            var gbiBankAccounts = TryDeserializeXml<GbiBankAccounts>(importXmlRequest.XmlString);
            if (gbiBankAccounts != null)
            {
                return SaveAndExportNewBankAccounts(gbiBankAccounts);
            }
            var gbiCustomers = TryDeserializeXml<GbiCustomers>(importXmlRequest.XmlString);
            if (gbiCustomers != null)
            {
                return SaveAndExportNewCustomers(gbiCustomers);
            }
            var gbiStatements = TryDeserializeXml<GbiStatements>(importXmlRequest.XmlString);
            if (gbiStatements != null)
            {
                return SaveStatements(gbiStatements);
            }
            var gbiInvoices = TryDeserializeXml<GbiInvoices>(importXmlRequest.XmlString);
            if (gbiInvoices != null)
            {
                return SaveInvoices(gbiInvoices);
            }

            response.ResponseMessage = "XML format not recognised";
            response.StatusCode = HttpStatusCode.BadRequest;
            return response;
        }

        private TrmResponse SaveAndExportNewCustomers(GbiCustomers gbiCustomers)
        {
            var response = new TrmResponse();
            var alreadyACustomer = 0;
            var successfullyAdded = 0;
            var failedToAdd = 0;

            int bullionPremiumGroupDefaultValue;
            var parseSuccess = int.TryParse(_bullionPremiumGroupHelper.GetBullionPremiumGroup().FirstOrDefault()?.Value,
                out bullionPremiumGroupDefaultValue);

            var bullionObsNumbers = gbiCustomers.Customer.Select(x => x.AxAccountNumber).Distinct();
            var customers = _customerContactService.GetAllContactsByBullionObsNumberList(bullionObsNumbers).ToList();
            foreach (var newCustomer in gbiCustomers.Customer)
            {
                var customer = customers.FirstOrDefault(x => x.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) == newCustomer.AxAccountNumber);
                if (customer != null)
                {
                    alreadyACustomer++;
                    continue;
                }

                var address = new AddressModel
                {
                    Name = StringConstants.PreciousMetalsRegisteredAddressName,
                    FirstName = newCustomer.Firstname,
                    LastName = newCustomer.Surname,
                    Email = newCustomer.Emailaddress,
                    Line1 = newCustomer.KycAddressLine1,
                    Line2 = newCustomer.KycAddressLine2,
                    City = newCustomer.KycAddressCity,
                    PostalCode = newCustomer.KycAddressPostcode,
                    CountryCode = newCustomer.KycCountryCode,
                    CountryRegion = new CountryRegionModel
                    {
                        Region = newCustomer.KycAddressCounty
                    },
                    BillingDefault = true,
                    ShippingDefault = true
                };

                var customerAddress = CustomerAddress.CreateInstance();
                customerAddress[StringConstants.AddressFieldNames.County] = newCustomer.KycAddressCounty;
                customerAddress[StringConstants.CustomFields.BullionKycAddress] = true;

                _addressBookService.MapToAddress(address, customerAddress);

                bool testBool;
                DateTime testDt;
                AccountKycStatus testKycStatus;
                if (!Enum.TryParse(newCustomer.KycStatus, out testKycStatus))
                {
                    testKycStatus = AccountKycStatus.PendingAdditionalInformation;
                }

                var twoPartQuestionId = _securityQuestionHelper.GetTwoPartQuestionId(newCustomer.SecurityQuestion);

                var user = new ApplicationUser
                {
                    UserName = string.IsNullOrEmpty(newCustomer.Username) ? newCustomer.Emailaddress : newCustomer.Username,
                    Email = newCustomer.Emailaddress,
                    Password = string.Empty,
                    Title = newCustomer.Title,
                    FirstName = newCustomer.Firstname,
                    LastName = newCustomer.Surname,
                    FullName = $"{newCustomer.Firstname} {newCustomer.Surname}",
                    MiddleName = newCustomer.MiddleName,
                    Gender = newCustomer.Gender,
                    BirthDate = DateTime.TryParse(newCustomer.DateOfBirth, out testDt) ? testDt : (DateTime?)null,
                    RegistrationSource = StringConstants.RegistrationSource.ImportedFromGbi,
                    ByEmail = bool.TryParse(newCustomer.ContactbyEmail, out testBool) && testBool,
                    EmailConsentSource = newCustomer.ContactbyEmailSource,
                    EmailConsentDateTime = DateTime.TryParse(newCustomer.ContactbyEmailDate, out testDt)
                        ? testDt
                        : DateTime.UtcNow,
                    ByPost = bool.TryParse(newCustomer.ContactbyPost, out testBool) && testBool,
                    PostConsentSource = newCustomer.ContactbyPostSource,
                    PostConsentDateTime = DateTime.TryParse(newCustomer.ContactbyPostDate, out testDt)
                        ? testDt
                        : DateTime.UtcNow,
                    ByTelephone = bool.TryParse(newCustomer.ContactbyPhone, out testBool) && testBool,
                    TelephoneConsentSource = newCustomer.ContactbyPhoneSource,
                    TelephoneConsentDateTime = DateTime.TryParse(newCustomer.ContactbyPhoneDate, out testDt)
                        ? testDt
                        : DateTime.UtcNow,
                    PhoneNumber = newCustomer.Telephonenumber,
                    MobilePhone = newCustomer.MobileTelephone,
                    Addresses = new List<CustomerAddress>(new[] { customerAddress }),
                    CustomerType = StringConstants.CustomerType.Bullion,
                    SecondSurname = newCustomer.Secondsurname,
                    CustomerKycData = new KycQueryResultDto
                    {
                        Status = testKycStatus
                    },
                    BullionCustomerType = bool.TryParse(newCustomer.SippOrSassCustomer, out testBool) && testBool ?
                        ((int)Enums.BullionCustomerType.SIPPCustomer).ToString() :
                        ((int)Enums.BullionCustomerType.Standard).ToString(),
                    TwoStepAuthenticationQuestion = twoPartQuestionId.ToString(),
                    TwoStepAuthenticationAnswer = newCustomer.SecurityAnswer,
                    Currency = newCustomer.Currency,
                    BullionAccountNumber = newCustomer.AxAccountNumber,
                    IsActivated = false,
                    IsSiteCoreUser = true,
                    BullionPremiumGroupInt = parseSuccess ? bullionPremiumGroupDefaultValue : 0,
                };

                var result = _userService.RegisterAccount(user, true);
                customer = result.Contact;
                if (customer != null && twoPartQuestionId > 0)
                {
                    _customersExportTransactionService.SaveBullionCustomerExportTransaction(customer, ExportTransactionType.UpdateBullion);
                    successfullyAdded++;
                    continue;
                }

                failedToAdd++;
            }

            response.ResponseMessage = $"{successfullyAdded} customers added sucessfully. {failedToAdd} customers failed to add. {alreadyACustomer} existing customers in this file.";
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }

        private TrmResponse SaveAndExportNewBankAccounts(GbiBankAccounts gbiBankAccounts)
        {
            var response = new TrmResponse();
            var noCustomer = 0;
            var failedToAddExportTransaction = 0;
            var successfullyAdded = 0;

            var customerCodes = gbiBankAccounts.BankAccount.Select(x => x.AxAccountNumber).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);

            foreach (var bankAccount in gbiBankAccounts.BankAccount)
            {
                var customer = customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(bankAccount.AxAccountNumber));

                if (customer == null)
                {
                    noCustomer++;
                    continue;
                }

                var customerBankAccountId = Guid.NewGuid();
                var bankAccountInformation = _customerBankAccountHelper.GetBankAccountInformation(
                    bankAccount.BankCountryCode,
                    bankAccount.UkSortCode,
                    bankAccount.Iban,
                    bankAccount.UkAccountNumber,
                    bankAccount.SwiftAccountNumber);
                _customerBankAccountHelper.SaveAccount(customer.Code, bankAccount.BankCountryCode, bankAccount.Nickname, bankAccount.AccountHolderName, bankAccount.Nickname, customerBankAccountId, bankAccountInformation);

                var bankAccountExportTransactionPayload = new BankAccountExportTransactionPayload
                {
                    Account = new ExportTransactionBankAccount
                    {
                        Action = TransactionOperation.Add.ToString("G"),
                        AxBankAccountId = customerBankAccountId.ToString("N").Substring(0, 10),
                        AccountNickname = bankAccount.Nickname,
                        AccountNumber = bankAccount.UkAccountNumber,
                        Iban = bankAccount.Iban,
                        SortCode = bankAccount.UkSortCode,
                        SwiftOrBic = bankAccount.SwiftAccountNumber,
                        BankCountry = bankAccount.BankCountryCode
                    }
                };

                var transactionAdded = _exportTransactionService.CreateExportTransaction(bankAccountExportTransactionPayload, customer.ContactId.ToString(), ExportTransactionType.BankAccount);

                if (!transactionAdded)
                {
                    failedToAddExportTransaction++;
                    continue;
                }

                successfullyAdded++;
            }

            response.ResponseMessage = $"{successfullyAdded} Bank Accounts added. {noCustomer} couldn't find a customer from the AX Account Number supplied, {failedToAddExportTransaction} failed to save a transaction to export.";
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }

        private TrmResponse SaveInvoices(GbiInvoices gbiInvoices)
        {
            var response = new TrmResponse();
            var noCustomer = 0;
            var successfullyAdded = 0;

            var customerCodes = gbiInvoices.Invoice.Select(x => x.AxAccountNumber).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);

            foreach (var invoice in gbiInvoices.Invoice)
            {
                var customer = customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(invoice.AxAccountNumber));

                if (customer == null)
                {
                    noCustomer++;
                    continue;
                }

                var customerId = customer.ContactId;
                _bullionDocumentRepository.ImportDocumentFromGbi(invoice, customerId);

                successfullyAdded++;
            }

            response.ResponseMessage = $"{successfullyAdded} Invoices added. {noCustomer} couldn't find a customer from the AX Account Number supplied.";
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }

        private TrmResponse SaveStatements(GbiStatements gbiStatements)
        {
            var response = new TrmResponse();
            var noCustomer = 0;
            var successfullyAdded = 0;

            var customerCodes = gbiStatements.Statement.Select(x => x.AxAccountNumber).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);

            foreach (var statement in gbiStatements.Statement)
            {
                var customer = customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(statement.AxAccountNumber));

                if (customer == null)
                {
                    noCustomer++;
                    continue;
                }

                var customerId = customer.ContactId;
                _bullionDocumentRepository.ImportDocumentFromGbi(statement, customerId);

                successfullyAdded++;
            }

            response.ResponseMessage = $"{successfullyAdded} Statements added. {noCustomer} couldn't find a customer from the AX Account Number supplied.";
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
    }
}