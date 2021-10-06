using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Logging;
using Mediachase.Commerce.Catalog;
using TRM.IntegrationServices.Models;
using TRM.IntegrationServices.Models.Import;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Business.Email;
using TRM.Web.Controllers.Services;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Services.DataImportCustomerService;
using TRM.Web.Services.Inventory;
using TRM.Web.Services.InvoiceStatements;
using TRM.Web.Services.Portfolio;

namespace TRM.Web.Services.Import
{
    public class AxDataImportService : BaseXmlImportService, IAxDataImportService
    {
        private readonly ICustomerContactService _customerContactService;
        private readonly IBullionStatementService _bullionStatementService;
        private readonly IBullionInvoiceService _bullionInvoiceService;
        private readonly IBullionPortfolioService _bullionPortfolioService;
        private readonly IBullionUserService _bullionUserService;
        private readonly IBullionEmailHelper _bullionEmailHelper;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLoader _contentLoader;
        private readonly IDataImportInventoryService _dataImportInventoryService;
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;
        private readonly IDataImportCustomerService _customerService;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly ICheckAml _amlCheckService;
        protected readonly ILogger Logger = LogManager.GetLogger(typeof(AxDataImportService));

        public AxDataImportService(
            ICustomerContactService customerContactService,
            IBullionStatementService bullionStatementService,
            IBullionInvoiceService bullionInvoiceService,
            IBullionPortfolioService bullionPortfolioService,
            IBullionUserService bullionUserService,
            IBullionEmailHelper bullionEmailHelper,
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            IDataImportInventoryService dataImportInventoryService,
            IAmTransactionHistoryHelper transactionHistoryHelper,
            IDataImportCustomerService customerService,
            IAmBullionContactHelper bullionContactHelper,
            ICheckAml amlCheckService)
        {
            _customerContactService = customerContactService;
            _bullionStatementService = bullionStatementService;
            _bullionInvoiceService = bullionInvoiceService;
            _bullionPortfolioService = bullionPortfolioService;
            _bullionUserService = bullionUserService;
            _bullionEmailHelper = bullionEmailHelper;
            _referenceConverter = referenceConverter;
            _contentLoader = contentLoader;
            _dataImportInventoryService = dataImportInventoryService;
            _transactionHistoryHelper = transactionHistoryHelper;
            _customerService = customerService;
            _bullionContactHelper = bullionContactHelper;
            _amlCheckService = amlCheckService;
        }

        public ImportXmlResponse ImportDataFromAx(ImportXmlRequest importXmlRequest)
        {
            var transactions = TryDeserializeXml<AxImportData.BullionTransactionHistory>(importXmlRequest.XmlString);
            if (transactions != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportNewTransactions(transactions));
            }

            var transactionStatusUpdate = TryDeserializeXml<AxImportData.TransactionStatusUpdate>(importXmlRequest.XmlString);
            if (transactionStatusUpdate != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportToUpdateTransactionStatuses(transactionStatusUpdate));
            }

            var bullionRetailCustomerBalances = TryDeserializeXml<AxImportData.BullionRetailCustomerBalances>(importXmlRequest.XmlString);
            if (bullionRetailCustomerBalances != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportBullionBalances(bullionRetailCustomerBalances));
            }

            var axHoldings = TryDeserializeXml<AxImportData.BullionHoldings>(importXmlRequest.XmlString);
            if (axHoldings != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportHoldings(axHoldings));
            }

            var axStatements = TryDeserializeXml<AxImportData.CustomerStmts>(importXmlRequest.XmlString);
            if (axStatements != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportBullionStatements(axStatements));
            }

            var axInvoices = TryDeserializeXml<AxImportData.Invoices>(importXmlRequest.XmlString);
            if (axInvoices != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportInvoices(axInvoices));
            }

            var bullionStock = TryDeserializeXml<AxImportData.BullionStock>(importXmlRequest.XmlString);
            if (bullionStock != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportInventories(bullionStock));
            }

            /*
            var customers = TryDeserializeXml<AxImportData.AxCustomerTables>(importXmlRequest.XmlString);
            if (customers != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportCustomers(customers));
            }
            */
            var customers = TryDeserializeXml<AxImportData.CustomersRequest>(importXmlRequest.XmlString);
            if (customers != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, ImportCustomers(customers));
            }
            var contacts = TryDeserializeXml<AxImportData.CustomerContactsRequest>(importXmlRequest.XmlString);
            if (contacts != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, UpdateContact(contacts.CustomerContacts));
            }
            var addressesRequest = TryDeserializeXml<AxImportData.AddressesRequest>(importXmlRequest.XmlString);
            if (addressesRequest != null)
            {
                return ProcessXmlUpdates(importXmlRequest.BatchId, UpdateAddresses(addressesRequest.Addresses));
            }

            return new ImportXmlResponse
            {
                ResponseResult = new TrmResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ResponseMessage = $"AxDataImport batch: {importXmlRequest.BatchId} - XML format not recognised"
                }
            };
        }

        //Import to update bullion contact's balances
        private List<XmlUpdateFromAx> ImportBullionBalances(AxImportData.BullionRetailCustomerBalances bullionRetailCustomerBalances)
        {
            var balances = new AxImportData.CustomerBalances
            {
                CustomerBalance = bullionRetailCustomerBalances.MagBullionBalanceTracking.Select(x =>
                    new AxImportData.CustomerBalance
                    {
                        CustomerCode = x.AccountNum,
                        Balances = new AxImportData.Balances
                        {
                            AvailableToSpend = x.AvailableForTrading.ToDecimalExactCulture(),
                            AvailableToWithdraw = x.AvailableForWithdrawal.ToDecimalExactCulture(),
                            Effective = x.EffectiveBalance.ToDecimalExactCulture()
                        }
                    }).ToArray()
            };

            var response = new List<XmlUpdateFromAx>();
            var customerCodes = balances.CustomerBalance.Select(x => x.CustomerCode).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);

            foreach (var balance in balances.CustomerBalance)
            {
                var xmlUpdate = new XmlUpdateFromAx
                {
                    AxCode = balance.CustomerCode
                };
                response.Add(xmlUpdate);

                try
                {
                    var customer = customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(balance.CustomerCode));

                    if (customer == null)
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.CustomerRecordNotFound.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    var customerId = customer.ContactId;

                    if (!_bullionContactHelper.IsBullionCustomerType(customer.CustomerCustomerType))
                    {
                        xmlUpdate.Message = "Not a bullion account";
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.CustomerRecordNotFound.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    if (!_bullionUserService.UpdateBullionCustomerBalances(balance, customerId))
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.CustomerRecordNotFound.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                    xmlUpdate.IsSuccess = true;
                }
                catch (Exception err)
                {
                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                    xmlUpdate.Exception = err;
                }
            }

            return response;
        }

        //Import bullion contact's statements
        private List<XmlUpdateFromAx> ImportBullionStatements(AxImportData.CustomerStmts statements)
        {
            Logger.Error($"Start Processing... - ImportBullionStatements" );
            var response = new List<XmlUpdateFromAx>();
            var customerCodes = statements.Statement.Select(x => x.CustomerCode).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);

            Logger.Error($"Start Processing Statements - Customer succesfully fetched." );
            foreach (var statement in statements.Statement)
            {
                var xmlUpdate = new XmlUpdateFromAx();
                response.Add(xmlUpdate);

                try
                {
                    xmlUpdate.AxCode = statement.CustomerCode;

                    var customer = customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(statement.CustomerCode));
                    if (customer == null)
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.CustomerRecordNotFound.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    var customerId = customer.ContactId;

                    if (_bullionStatementService.AddCustomerStatementFromImportData(statement, customerId))
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                        // send Email when added successfully
                        SendEmailBullionStatement(customerId, statement);
                        continue;
                    }

                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                }
                catch (Exception err)
                {
                    Logger.Error($"Exception - {err}" );
                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                    xmlUpdate.Exception = err;
                }
            }

            return response;
        }

        //Import bullion contact's invoices
        private List<XmlUpdateFromAx> ImportInvoices(AxImportData.Invoices invoices)
        {
            var response = new List<XmlUpdateFromAx>();
            var customerCodes = invoices.Invoice.Select(x => x.CustomerCode).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);

            foreach (var invoice in invoices.Invoice)
            {
                var xmlUpdate = new XmlUpdateFromAx
                {
                    AxCode = invoice.CustomerCode
                };
                response.Add(xmlUpdate);

                try
                {
                    var customer = customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(invoice.CustomerCode));

                    if (customer == null)
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.CustomerRecordNotFound.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    var customerId = customer.ContactId;

                    if (_bullionInvoiceService.AddCustomerInvoiceFromImportData(invoice, customerId))
                    {
                        _bullionEmailHelper.SendBullionInvoiceAvailableEmail(customerId, invoice.InvoiceDate.ToSqlDatetime());

                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                        continue;
                    }

                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                }
                catch (Exception err)
                {
                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                    xmlUpdate.Exception = err;
                }
            }

            return response;
        }

        //Import bullion contact's vault holdings (Portfolio)
        private List<XmlUpdateFromAx> ImportHoldings(AxImportData.BullionHoldings holdings)
        {
            var response = new List<XmlUpdateFromAx>();
            var customerCodes = holdings.Customer.Select(x => x.CustomerCode).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);

            foreach (var axCustomer in holdings.Customer)
            {
                var xmlUpdate = new XmlUpdateFromAx
                {
                    AxCode = axCustomer.CustomerCode
                };
                response.Add(xmlUpdate);

                try
                {
                    var customer = customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(axCustomer.CustomerCode));

                    if (customer == null)
                    {
                        xmlUpdate.Message = $"The Customer has Code {axCustomer.CustomerCode}  Not Found In Epi";
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.CustomerRecordNotFound.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    var customerId = customer.ContactId;

                    if (_bullionPortfolioService.HasOutstandingBuyOrSellOrDeliverTransaction(customerId))
                    {
                        xmlUpdate.Message =
                            "Bullion Holdings cannot import because customer has outstanding transactions.";
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                        continue;
                    }

                    if (_bullionPortfolioService.ImportPortfolioFromAx(axCustomer, customerId))
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                        continue;
                    }

                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                }
                catch (Exception err)
                {
                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                    xmlUpdate.Exception = err;
                }
            }

            return response;
        }

        //Import to update inventories
        private List<XmlUpdateFromAx> ImportInventories(AxImportData.BullionStock bullionStock)
        {
            var inventories = new AxImportData.Inventories
            {
                ItemInventories = bullionStock.InventSum.Select(x => new AxImportData.ItemInventory
                {
                    ItemId = x.ItemId,
                    AvailablePhysical = x.AvailPhysical.ToDecimalExactCulture(),
                    AvailableToOrder = x.AvailabeToOrder.ToDecimalExactCulture()
                }).ToList()
            };

            var response = new List<XmlUpdateFromAx>();

            foreach (var inventory in inventories.ItemInventories)
            {
                var xmlUpdate = new XmlUpdateFromAx
                {
                    AxCode = inventory.ItemId
                };
                response.Add(xmlUpdate);

                try
                {
                    var entryContentLink = _referenceConverter.GetContentLink(inventory.ItemId);
                    var entryContentBase = _contentLoader.Get<EntryContentBase>(entryContentLink);
                    if (entryContentBase == null)
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.EntryRecordNotFound.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    var premiumVariant = entryContentBase as IAmPremiumVariant;
                    if (premiumVariant == null)
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.EntryRecordNotBullion.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    if (_dataImportInventoryService.SaveInventoryFromAx(inventory, premiumVariant))
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                        continue;
                    }

                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                }
                catch (Exception err)
                {
                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                    xmlUpdate.Exception = err;
                }
            }

            return response;
        }

        //Import to update bullion contact's transaction status
        private List<XmlUpdateFromAx> ImportToUpdateTransactionStatuses(AxImportData.TransactionStatusUpdate transactionStatusUpdate)
        {
            var response = new List<XmlUpdateFromAx>();
            var customerCodes = transactionStatusUpdate.Customer.Select(x => x.CustomerCode).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);
            var checkToUpdateTransations = new List<Guid>();
            Guid checkToUpdateTransation;

            foreach (var axCustomer in transactionStatusUpdate.Customer)
            {
                var customer =
                    customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(axCustomer.CustomerCode));

                if (customer == null)
                {
                    response.Add(new XmlUpdateFromAx
                    {
                        AxCode = axCustomer.CustomerCode,
                        ExportStatus = Enums.eXmlUpdateStatus.CustomerRecordNotFound.ToString(),
                        IsSuccess = false
                    }
                    );
                    continue;
                }

                var customerId = customer.ContactId;

                foreach (var transaction in axCustomer.Transactions)
                {
                    var xmlUpdate = new XmlUpdateFromAx
                    {
                        AxCode = $"{axCustomer.CustomerCode}::{transaction.TransactionId}"
                    };

                    try
                    {
                        if (!_transactionHistoryHelper.UpdateTransactionLineItemStatusFromAx(transaction, customerId, out checkToUpdateTransation))
                        {
                            xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                            xmlUpdate.IsSuccess = false;
                            response.Add(xmlUpdate);
                            continue;
                        }

                        if (checkToUpdateTransation != Guid.Empty)
                        {
                            checkToUpdateTransations.Add(checkToUpdateTransation);
                        }

                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                    }
                    catch (Exception err)
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                        xmlUpdate.IsSuccess = false;
                        xmlUpdate.Exception = err;
                    }
                    response.Add(xmlUpdate);
                }

                if (checkToUpdateTransations.Any())
                {
                    _transactionHistoryHelper.UpdateTransactionHistoryStatus(checkToUpdateTransations.Distinct());
                }
            }

            return response;
        }

        //Import the new transactions: Bank Deposit, Balance Adjustment, Storage Fee
        private List<XmlUpdateFromAx> ImportNewTransactions(AxImportData.BullionTransactionHistory axBullionTransactionHistory)
        {
            var response = new List<XmlUpdateFromAx>();
            var customerCodes = axBullionTransactionHistory.CustomerTransactions.Select(x => x.CustomerCode).Distinct();
            var customers = _customerContactService.GetAllContactsFromBullionObsAccountNumberList(customerCodes);

            foreach (var axCustomer in axBullionTransactionHistory.CustomerTransactions)
            {
                try
                {
                    var customer = customers.FirstOrDefault(x => x.CustomerBullionObsAccountNumber.Equals(axCustomer.CustomerCode));

                    if (customer == null)
                    {
                        response.Add(new XmlUpdateFromAx
                        {
                            AxCode = axCustomer.CustomerCode,
                            ExportStatus = Enums.eXmlUpdateStatus.CustomerRecordNotFound.ToString(),
                            IsSuccess = false
                        });
                        continue;
                    }

                    foreach (var axCustomerTransaction in axCustomer.Transactions)
                    {
                        if (_transactionHistoryHelper.ImportTransactionHistoryFromAx(axCustomerTransaction, customer))
                        {
                            response.Add(new XmlUpdateFromAx
                            {
                                AxCode = $"{axCustomer.CustomerCode}::{axCustomerTransaction.AXTransactionId}",
                                ExportStatus = Enums.eXmlUpdateStatus.Success.ToString(),
                                IsSuccess = true
                            });
                        }
                        else
                        {
                            response.Add(new XmlUpdateFromAx
                            {
                                AxCode = $"{axCustomer.CustomerCode}::{axCustomerTransaction.AXTransactionId}",
                                ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString(),
                                IsSuccess = false
                            });
                        }
                    }

                    if (axCustomer.Transactions.Any(x =>
                        x.Type.ToLower().Contains("bank") ||
                        x.Type.ToLower().Contains("balance")))
                    {
                        _amlCheckService.ForCustomer(customer.ContactId);
                    }
                }
                catch (Exception err)
                {
                    response.Add(new XmlUpdateFromAx
                    {
                        AxCode = $"{axCustomer.CustomerCode}",
                        ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString(),
                        IsSuccess = false,
                        Exception = err
                    });
                }
            }

            return response;
        }

        private List<XmlUpdateFromAx> ImportCustomers(AxImportData.CustomersRequest axCustomers)
        {
            var response = new List<XmlUpdateFromAx>();

            var validCustomerRequests = axCustomers.CustomerRequests
                .Where(x => !string.IsNullOrWhiteSpace(x.AccountNum)).ToList();

            var obsNumbers = validCustomerRequests.Select(x => x.AccountNum);
            var customers = _customerContactService.GetAllContactsByObsNumberList(obsNumbers).ToList();
            foreach (var axCustomer in validCustomerRequests)
            {
                var xmlUpdate = new XmlUpdateFromAx
                {
                    AxCode = axCustomer.AccountNum
                };
                response.Add(xmlUpdate);

                try
                {
                    var customer = customers.FirstOrDefault(x => x.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) == axCustomer.AccountNum ||
                                                                 x.GetStringProperty(StringConstants.CustomFields.ObsAccountNumber) == axCustomer.AccountNum);

                    if (customer == null)
                    {
                        //if (_customerService.CreateCustomerFromAx(axCustomer))
                        //{
                        //    xmlUpdate.Message = "New customer created";
                        //    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        //    xmlUpdate.IsSuccess = true;
                        //    continue;
                        //}
                        xmlUpdate.Message = "We really shouldn't be supporting create bullion customers from AX....";
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    if (_customerService.UpdateCustomerFromAx(axCustomer, customer))
                    {
                        xmlUpdate.Message = "Existing customer updated";
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                        continue;
                    }

                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                }
                catch (Exception err)
                {
                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                    xmlUpdate.Exception = err;
                }
            }

            return response;
        }

        private List<XmlUpdateFromAx> UpdateContact(AxImportData.CustomerContacts axContactRequests)
        {
            var response = new List<XmlUpdateFromAx>();

            var obsNumbers = axContactRequests.ObsCustomerContactRequests.Select(x => x.ObsAccountNumber);
            var customers = _customerContactService.GetAllContactsByObsNumberList(obsNumbers).ToList();
            foreach (var axContact in axContactRequests.ObsCustomerContactRequests)
            {
                var xmlUpdate = new XmlUpdateFromAx
                {
                    AxCode = axContact.ObsAccountNumber
                };
                response.Add(xmlUpdate);

                try
                {
                    var customer = customers.FirstOrDefault(x => x.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) == axContact.ObsAccountNumber ||
                                                                 x.GetStringProperty(StringConstants.CustomFields.ObsAccountNumber) == axContact.ObsAccountNumber);

                    if (customer == null)
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    if (_customerService.UpdateContactFromAx(axContact, customer))
                    {
                        xmlUpdate.Message = "Existing customer updated";
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                        continue;
                    }


                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                }
                catch (Exception err)
                {
                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                    xmlUpdate.Exception = err;
                }
            }

            return response;
        }

        private List<XmlUpdateFromAx> UpdateAddresses(AxImportData.Addresses axContactAddresses)
        {
            var response = new List<XmlUpdateFromAx>();

            var obsNumbers = axContactAddresses.AddressRequest.Select(x => x.AccountNum);
            var customers = _customerContactService.GetAllContactsByObsNumberList(obsNumbers).ToList();

            var groupedAddresses = axContactAddresses.AddressRequest.GroupBy(x => x.AccountNum);

            foreach (var axAddressGrouping in groupedAddresses)
            {
                var xmlUpdate = new XmlUpdateFromAx
                {
                    AxCode = axAddressGrouping.Key
                };
                response.Add(xmlUpdate);

                try
                {
                    var customer = customers.FirstOrDefault(x => x.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) == axAddressGrouping.Key ||
                                                                 x.GetStringProperty(StringConstants.CustomFields.ObsAccountNumber) == axAddressGrouping.Key);

                    var isBullionUser = axAddressGrouping.Key ==
                                        customer.GetStringProperty(StringConstants.CustomFields
                                            .BullionObsAccountNumber);

                    if (customer == null)
                    {
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                        xmlUpdate.IsSuccess = false;
                        continue;
                    }

                    if (_customerService.UpdateCustomerAddressesFromAx(axAddressGrouping.AsEnumerable().ToList(), customer, isBullionUser))
                    {
                        xmlUpdate.Message = "Existing customer updated";
                        xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.Success.ToString();
                        xmlUpdate.IsSuccess = true;
                        continue;
                    }


                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                }
                catch (Exception err)
                {
                    xmlUpdate.ExportStatus = Enums.eXmlUpdateStatus.FailedInEpi.ToString();
                    xmlUpdate.IsSuccess = false;
                    xmlUpdate.Exception = err;
                }
            }

            return response;
        }

        private void SendEmailBullionStatement(Guid customerId, AxImportData.CustomerStmtsStatement customerStatement)
        {
            DateTime outputDateTime;
            var statementDate = customerStatement.ToDate.TryParseDatetimeExact("yyyy-MM-dd", out outputDateTime) ? outputDateTime : DateTime.Now;
            _bullionEmailHelper.SendBullionStatementAvailableEmail(customerId, statementDate);
        }
    }
}