using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Castle.Core.Internal;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Logging.Compatibility;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders.Managers;
using TrmWallet;
using TRM.IntegrationServices.Models.Export.Emails;
using TRM.IntegrationServices.Models.Export.Orders;
using TRM.Shared.Extensions;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Email;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers.TransactionHistories;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.EntityFramework.BullionPortfolio;
using TRM.Web.Models.EntityFramework.Transactions;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Services;
using static TRM.Web.Constants.Enums;
using static TRM.Shared.Constants.StringConstants;
using System.Globalization;

namespace TRM.Web.Helpers
{
    public class TransactionHistoryHelper : IAmTransactionHistoryHelper
    {
        private const int DefaultPageSize = 10;

        private static ILog Logger = LogManager.GetLogger(typeof(TransactionHistoryRepository));
        private readonly ITransactionHistoryRepository _transactionHistoryRepository;
        private readonly CustomerContext _customerContext;
        private readonly IAmEntryHelper _entryHelper;
        private readonly IUserService _userService;
        private readonly LocalizationService _localizationService;
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly ITrmWalletRepository _walletRepository;
        private readonly IEnumerable<ITransactionHistoryDetailBuilderHelper> _transactionHistoryDetailBuilderHelpers;
        private readonly IBullionEmailHelper _bullionEmailHelper;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public TransactionHistoryHelper(
            ITransactionHistoryRepository transactionHistoryRepository,
            CustomerContext customerContext,
            IAmEntryHelper entryHelper,
            LocalizationService localizationService,
            IUserService userService,
            IPurchaseOrderRepository orderRepository,
            ITrmWalletRepository walletRepository,
            IEnumerable<ITransactionHistoryDetailBuilderHelper> transactionHistoryDetailBuilderHelpers,
            IBullionEmailHelper bullionEmailHelper, IAmBullionContactHelper bullionContactHelper)
        {
            _transactionHistoryRepository = transactionHistoryRepository;
            _customerContext = customerContext;
            _entryHelper = entryHelper;
            _localizationService = localizationService;
            _transactionHistoryDetailBuilderHelpers = transactionHistoryDetailBuilderHelpers;
            _userService = userService;
            _orderRepository = orderRepository;
            _walletRepository = walletRepository;
            _bullionEmailHelper = bullionEmailHelper;
            _bullionContactHelper = bullionContactHelper;
        }

        public IEnumerable<DormentFundsItem> GetCustomerByDormantFunds(int dormentDays)
        {
            using (var db = new TransactionHistoriesContext())
            {
                var baselineDate = DateTime.Now.AddDays(-dormentDays);
                Expression<Func<TransactionHistory, bool>> predicate = x => x.Balance > 0 && x.CreatedDate > baselineDate;
                var returnCount = db.TransactionHistory.Count(predicate);
                if (returnCount == 0) return Enumerable.Empty<DormentFundsItem>();

                var filteredTransactions = db.TransactionHistory
                    .Where(predicate)
                    .GroupBy(x => x.CustomerRef)
                    .Where(x => x.All(y => y.TransactionType != TransactionHistoryType.Purchase.ToString()))
                    .Select(x => x.OrderByDescending(y => y.CreatedDate))
                    .ToList();

                if (filteredTransactions.IsNullOrEmpty()) return Enumerable.Empty<DormentFundsItem>();

                List<DormentFundsItem> dormentFunds = filteredTransactions
                    .Select(x => new DormentFundsItem { CustomerRef = x.First().CustomerRef, TransactionDate = x.First().CreatedDate })
                    .ToList();

                return dormentFunds;
            }
        }

        public Dictionary<TransactionHistoryType, string> GetTransactionFilterOptions()
        {
            return new Dictionary<TransactionHistoryType, string>
            {
                { TransactionHistoryType.FundsAddedCard, GetLocalizationKey(TransactionHistoryType.FundsAddedCard) },
                { TransactionHistoryType.Purchase, GetLocalizationKey(TransactionHistoryType.Purchase) },
                { TransactionHistoryType.Withdraw, GetLocalizationKey(TransactionHistoryType.Withdraw) },
                { TransactionHistoryType.DeliverFromVault, GetLocalizationKey(TransactionHistoryType.DeliverFromVault) },
                { TransactionHistoryType.SellFromVault, GetLocalizationKey(TransactionHistoryType.SellFromVault) },
                { TransactionHistoryType.StorageFee, GetLocalizationKey(TransactionHistoryType.StorageFee) },
                { TransactionHistoryType.AccountActivity, GetLocalizationKey(TransactionHistoryType.AccountActivity) },
                { TransactionHistoryType.BalanceAdjustment, GetLocalizationKey(TransactionHistoryType.BalanceAdjustment) },
                { TransactionHistoryType.BankDeposit, GetLocalizationKey(TransactionHistoryType.BankDeposit) }
            };
        }

        public decimal GetTransactionHistoricalSpend(string userId, DateTime since)
        {
            if (string.IsNullOrEmpty(userId)) return 0m;

            using (var db = new TransactionHistoriesContext())
            {
                db.Database.CommandTimeout = 60000;

                var query = (
                    from o in db.TransactionHistory
                    where (
                        o.TransactionType == TransactionHistoryType.BankDeposit.ToString() || 
                        o.TransactionType == TransactionHistoryType.FundsAddedCard.ToString() ||
                        o.TransactionType == TransactionHistoryType.BalanceAdjustment.ToString()) &&
                        o.CustomerRef == userId && 
                        o.CreatedDate > since
                    select o.Amount).DefaultIfEmpty();

                return (decimal?)query.Sum() ?? 0;
            }
        }

        public IEnumerable<TransactionHistoryItemViewModel> GetTransactionHistories(
            string customerId,
            TransactionHistoryType filter,
            int currentPageNumber,
            int pageSize,
            out int totalRecords)
        {
            totalRecords = 0;
            pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
            if (string.IsNullOrEmpty(customerId)) return new List<TransactionHistoryItemViewModel>();

            using (var db = new TransactionHistoriesContext())
            {
                Expression<Func<TransactionHistory, bool>> predicate = x => filter != TransactionHistoryType.All
                    ? x.CustomerRef == customerId && x.TransactionType == filter.ToString()
                    : x.CustomerRef == customerId;
                totalRecords = db.TransactionHistory.Count(predicate);
                if (totalRecords == 0) return Enumerable.Empty<TransactionHistoryItemViewModel>();

                var allRecordsForThisPage = db.TransactionHistory
                    .Include(x => x.TransactionHistoryOrderLines)
                    .Where(predicate)
                    .OrderByDescending(x => x.CreatedDate)
                    .Skip(GetSkip(currentPageNumber, pageSize))
                    .Take(pageSize)
                    .ToList();

                if (allRecordsForThisPage.IsNullOrEmpty()) return Enumerable.Empty<TransactionHistoryItemViewModel>();
                var transactionHistoryTypeTextMappings = GetTransactionTypeWithDescriptionMappings();

                return allRecordsForThisPage.Select(x => ConvertToViewModel(x, transactionHistoryTypeTextMappings));
            }
        }

        public bool LogTheAddFundTransaction(decimal amount, decimal newCustomerAvailableBalance,
            string currencyCode, string orderNumber, string exportTransactionId, string clientIpAddress = "")
        {
            try
            {
                var transactionHistoryRecord = new TransactionHistory(TransactionHistoryType.FundsAddedCard)
                {
                    CurrencyCode = currencyCode,
                    OrderNumber = orderNumber,
                    ExportTransactionId = exportTransactionId,
                    ClientIpAddress = clientIpAddress
                };
                transactionHistoryRecord = transactionHistoryRecord.SetContactInfo(_customerContext.CurrentContactId, _userService.GetImpersonateUser())
                    .SetAmountAndBalance(amount, newCustomerAvailableBalance);

                transactionHistoryRecord.TransactionHistoryOrderLines.Add(new TransactionHistoryOrderLine()
                {
                    PkId = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    LineItemId = exportTransactionId,
                    VariantCode = orderNumber,
                    Total = amount,
                    LineStatus = TransactionHistoryStatus.Inprogress.GetDescriptionAttribute()
                });

                if (!_transactionHistoryRepository.AddOrUpdateTransactionHistoryRecord(transactionHistoryRecord))
                {
                    throw new Exception($"Cannot insert the transaction history {transactionHistoryRecord.TransactionType} record");
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }

        public bool LogThePurchaseTransaction(IPurchaseOrder purchaseOrder, string clientIpAddress = "")
        {
            return LogThePurchaseTransaction(purchaseOrder, null, clientIpAddress);
        }
        public bool LogThePurchaseTransaction(IPurchaseOrder purchaseOrder, CustomerContact customerContact, string clientIpAddress = "")
        {
            try
            {
                var allLineItems = purchaseOrder?.GetAllLineItems();
                if (purchaseOrder == null || allLineItems.IsNullOrEmpty())
                    throw new Exception("Cannot log the Transaction History for null/empty purchase Order");
                var purchaseTransactionHistoryRecord = BuildPurchaseTransactionHistoryRecord(purchaseOrder, customerContact, clientIpAddress);

                var insertSuccess =
                    _transactionHistoryRepository.AddOrUpdateTransactionHistoryRecord(purchaseTransactionHistoryRecord);
                if (!insertSuccess)
                    throw new Exception($"Cannot insert the transaction history record for order {purchaseOrder.OrderNumber}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }

        public bool LogTheSellFromVaultTransactionHistory(TrmSellTransaction sellTransaction)
        {
            try
            {
                if (_customerContext.CurrentContact == null) throw new Exception("Transaction History Feature supports the logged user only.");

                var orderNumber = $"{sellTransaction.TransactionNumberPrefix}{sellTransaction.TransactionNumber}";
                var transactionToRecord = new TransactionHistory(TransactionHistoryType.SellFromVault)
                {
                    Status = sellTransaction.Status,
                    OrderNumber = orderNumber,
                    CurrencyCode = sellTransaction.Currency,
                    ExportTransactionId = sellTransaction.ExportTransactionId
                };
                var totalAmount = sellTransaction.MetalPrice - sellTransaction.SellPremium;
                transactionToRecord = transactionToRecord
                    .SetContactInfo(_customerContext.CurrentContactId, _userService.GetImpersonateUser())
                    .SetAmountAndBalance(totalAmount, _bullionContactHelper.GetEffectiveBalance(_customerContext.CurrentContact));

                transactionToRecord.TransactionHistoryOrderLines.Add(new TransactionHistoryOrderLine
                {
                    OrderNumber = orderNumber,
                    LineItemId = $"Li-{orderNumber}-{transactionToRecord.TransactionHistoryOrderLines.Count + 1}",
                    Quantity = sellTransaction.RequestedQuantityToSell,
                    VariantCode = sellTransaction.VariantCode,
                    Total = totalAmount,
                    PkId = Guid.NewGuid(),
                    LineStatus = TransactionHistoryStatus.Inprogress.GetDescriptionAttribute(),
                    MetalPrice = sellTransaction.MetalPrice,
                    ExVatLineTotalMetalPrice = sellTransaction.MetalPrice * sellTransaction.UnitMetalWeightOz,
                    ExVatLineTotalPremium = sellTransaction.SellPremium * sellTransaction.UnitMetalWeightOz,
                });
                var insertSuccess = _transactionHistoryRepository.AddOrUpdateTransactionHistoryRecord(transactionToRecord);

                if (!insertSuccess)
                    throw new Exception($"Cannot insert the transaction history record for recent sold item {sellTransaction.VariantCode}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
        }

        public bool LogTheDeliveryFromVaultTransactionHistory(TrmDeliverTransaction deliverTransaction)
        {
            try
            {
                if (_customerContext.CurrentContact == null) throw new Exception("Transaction History Feature supports the logged user only.");
                var deliveryFee = deliverTransaction.ShippingFee + deliverTransaction.ShippingFeeVAT;
                var orderNumber = $"{deliverTransaction.TransactionNumberPrefix}{deliverTransaction.TransactionNumber}";

                var currency = !string.IsNullOrEmpty(deliverTransaction.Currency) ? new Currency(deliverTransaction.Currency) : Currency.GBP;

                var transactionToRecord = new TransactionHistory(TransactionHistoryType.DeliverFromVault)
                {
                    OrderNumber = orderNumber,
                    CurrencyCode = deliverTransaction.Currency,
                    DeliveryFee = deliverTransaction.ShippingFee + deliverTransaction.ShippingFeeVAT,
                    ExportTransactionId = deliverTransaction.ExportTransactionId,
                    Status = deliveryFee == decimal.Zero ? TransactionHistoryStatus.Settled.GetDescriptionAttribute() : TransactionHistoryStatus.Inprogress.GetDescriptionAttribute(),
                    IncludedInAxBalances = deliveryFee == decimal.Zero
                };

                transactionToRecord = transactionToRecord
                    .SetContactInfo(_customerContext.CurrentContactId, _userService.GetImpersonateUser())
                    .SetAmountAndBalance(-deliverTransaction.TotalAmount, _bullionContactHelper.GetEffectiveBalance(_customerContext.CurrentContact));
                transactionToRecord.TransactionType = TransactionHistoryType.DeliverFromVault.ToString();

                var index = 1;
                foreach (var lineItem in deliverTransaction.OriginalPurchaseOrders)
                {
                    transactionToRecord.TransactionHistoryOrderLines.Add(new TransactionHistoryOrderLine
                    {
                        PkId = Guid.NewGuid(),
                        OrderNumber = orderNumber,
                        LineItemId = $"Li-{orderNumber}-{index++}",
                        Quantity = lineItem.QuantityToDeliver,
                        VariantCode = deliverTransaction.VariantCode,
                        Total = lineItem.VATToPay,
                        LineStatus = TransactionHistoryStatus.Settled.GetDescriptionAttribute(),
                    });
                }
                
                transactionToRecord.TransactionHistoryOrderLines.Add(new TransactionHistoryOrderLine
                {
                    PkId = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    LineItemId = $"Ch-{orderNumber}-1",
                    VariantCode = deliverTransaction.VariantCode,
                    Quantity = transactionToRecord.TransactionHistoryOrderLines.Sum(x => x.Quantity),
                    Total = deliveryFee,
                    LineStatus = deliveryFee == decimal.Zero ? TransactionHistoryStatus.Settled.GetDescriptionAttribute() : TransactionHistoryStatus.Inprogress.GetDescriptionAttribute(),
                    MetalPrice = null,
                    VatCost = deliverTransaction.VatRate,
                    VatStatus = deliverTransaction.VatCode,
                    DeliveryType = deliverTransaction.ShippingMethod,
                    ExVatLineTotalMetalPrice = null,
                    ExVatLineTotalPremium = null,
                });

                var insertSuccess = _transactionHistoryRepository.AddOrUpdateTransactionHistoryRecord(transactionToRecord);
                if (insertSuccess && deliveryFee == decimal.Zero)
                {
                    //send email dispatch from vault
                    _bullionEmailHelper.SendBullionDispatchFromVaultEmail(_customerContext.CurrentContactId, BuildDispatchFromVaultOrderDto(_customerContext.CurrentContactId, transactionToRecord));
                }
                if (!insertSuccess)
                    throw new Exception($"Cannot insert the transaction history record for recent deliveried item {deliverTransaction.VariantCode}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
        }

        public bool LogTheWithdrawTransactionHistory(string bankAccountId, string bankAccountNickName, decimal amountIncludingFee,
            string withdrawMethodName, decimal withdrawFee, string currencyCode, string exportTransactionId)
        {
            try
            {
                if (_customerContext.CurrentContact == null) throw new Exception("Transaction History Feature supports the logged user only.");
                var transactionToRecord = new TransactionHistory(TransactionHistoryType.Withdraw)
                {
                    OrderNumber = exportTransactionId,
                    Amount = -amountIncludingFee,
                    WithdrawalMethod = withdrawMethodName,
                    WithdrawalFee = withdrawFee,
                    BankAccount = bankAccountNickName,
                    CurrencyCode = currencyCode,
                    ExportTransactionId = exportTransactionId
                };

                transactionToRecord = transactionToRecord
                    .SetContactInfo(_customerContext.CurrentContactId, _userService.GetImpersonateUser())
                    .SetAmountAndBalance(-amountIncludingFee, _bullionContactHelper.GetEffectiveBalance(_customerContext.CurrentContact));

                transactionToRecord.TransactionHistoryOrderLines.Add(new TransactionHistoryOrderLine
                {
                    PkId = Guid.NewGuid(),
                    OrderNumber = exportTransactionId,
                    LineItemId = exportTransactionId,
                    VariantCode = bankAccountId,
                    Total = -amountIncludingFee,
                    LineStatus = TransactionHistoryStatus.Inprogress.GetDescriptionAttribute()
                });

                var insertSuccess = _transactionHistoryRepository.AddOrUpdateTransactionHistoryRecord(transactionToRecord);
                if (!insertSuccess)
                    throw new Exception($"Cannot insert the transaction history record for withdraw action for bank account {bankAccountNickName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }

        public bool ImportTransactionHistoryFromAx(AxImportData.AxTransactionItem transactionHistory,
            CustomerContactViewModel contact)
        {
            var epiContact = _customerContext.GetContactById(contact.ContactId);
            if (epiContact == null) return true;

            var transactionType = string.Empty;
            var submittedBy = string.Empty;
            var includedInAxBalances = false;
            var sendEmailBankDeposit = false;
            var amount = decimal.Parse(transactionHistory.Amount);
            var totalSpend = 
                epiContact.GetDecimalProperty(CustomFields.TotalSpendSinceKyc) + amount;
            
            if (transactionHistory.Type.Equals("Balance adjustment"))
            {
                //update three balances
                includedInAxBalances = true;
                submittedBy = StringConstants.TransactionHistoryPerformer.RoyalMint;
                transactionType = TransactionHistoryType.BalanceAdjustment.ToString();
                epiContact.UpdateProperty(CustomFields.TotalSpendSinceKyc, totalSpend);
                epiContact.SaveChanges();
            }
            else if (transactionHistory.Type.Equals("Bank deposit"))
            {
                //update effective balance
                includedInAxBalances = true;
                sendEmailBankDeposit = true;
                submittedBy = StringConstants.TransactionHistoryPerformer.AccountHolder;
                transactionType = TransactionHistoryType.BankDeposit.ToString();
                epiContact.UpdateProperty(CustomFields.TotalSpendSinceKyc, totalSpend);
                epiContact.SaveChanges();
            }
            else if (transactionHistory.Type.Equals("Storage invoice fee"))
            {
                includedInAxBalances = true;
                submittedBy = StringConstants.TransactionHistoryPerformer.RoyalMint;
                transactionType = TransactionHistoryType.StorageFee.ToString();
            }
            else if (transactionHistory.Type.Equals("Funds withdrawal"))
            {
                includedInAxBalances = true;
                submittedBy = StringConstants.TransactionHistoryPerformer.AccountHolder;
                transactionType = TransactionHistoryType.Withdraw.ToString();
            }

            var outputDateTime = transactionHistory.Date.ToSqlDatetime();

            var transactionToRecord = new TransactionHistory
            {
                PkId = Guid.NewGuid(),
                CurrencyCode = transactionHistory.Currency,
                CustomerRef = contact.ContactId.ToString(),
                OrderNumber = transactionHistory.AXTransactionId,
                //Amount = transactionHistory.Amount.ToDecimalExactCulture(), // contains buggy EPiServer code
                Amount = amount,
                CreatedDate = outputDateTime,
                PeriodFrom = outputDateTime,
                PeriodTo = outputDateTime,
                Description = transactionHistory.Description,
                TransactionType = transactionType,
                Status = transactionHistory.Status,
                IncludedInAxBalances = includedInAxBalances,
                SubmitedBy = submittedBy
            };

            var balance = _bullionContactHelper.GetEffectiveBalance(epiContact) + amount;

            transactionToRecord.SetAmountAndBalance(amount, balance);

            var transactionUpdated = _transactionHistoryRepository.AddOrUpdateTransactionHistoryRecord(transactionToRecord);
            if (!transactionUpdated) return false;

            if (transactionType == TransactionHistoryType.BankDeposit.ToString())
            {
                //update effective balance
                _bullionContactHelper.UpdateBalances(epiContact, amount, 0, 0);
            }

            if (transactionType == TransactionHistoryType.BalanceAdjustment.ToString() ||
                transactionType == TransactionHistoryType.StorageFee.ToString() ||
                transactionType == TransactionHistoryType.Withdraw.ToString())
            {
                //update three balances
                _bullionContactHelper.UpdateBalances(epiContact, amount, amount, amount);
            }

            if (sendEmailBankDeposit)
            {
                _bullionEmailHelper.SendBankTransferFundsNowAvailableEmail(contact.ContactId, BuildBankDepositModel(transactionToRecord, contact));
            }

            return true;
        }

        public bool UpdateTransactionLineItemStatusFromAx(AxImportData.TransactionStatusUpdateCustomerItem transactionStatusUpdateItem, Guid contactId, out Guid transactionHistoryId)
        {
            transactionHistoryId = Guid.Empty;
            var transactionLineItem =
                _transactionHistoryRepository.GetTransactionHistoryOrderLineByEpiTransId(
                    transactionStatusUpdateItem.TransactionId);
            if (transactionLineItem == null) return false;

            transactionHistoryId = transactionLineItem.TransactionHistoryId;
            if (transactionLineItem.LineStatus == transactionStatusUpdateItem.Status) return true;

            transactionLineItem.LineStatus = transactionStatusUpdateItem.Status;
            return _transactionHistoryRepository.UpsertTransactionHistoryOrderLineRecord(transactionLineItem);
        }

        public bool UpdateTransactionHistoryStatus(IEnumerable<Guid> transactionIds)
        {
            try
            {
                using (var db = new TransactionHistoriesContext())
                {
                    foreach (var transactionId in transactionIds)
                    {
                        var haveChange = false;
                        var transactionHistory = db.TransactionHistory.Include(x => x.TransactionHistoryOrderLines).FirstOrDefault(x => x.PkId == transactionId);

                        if (transactionHistory == null) continue;

                        //Set Header Status based on rules
                        var transactionLineItems = transactionHistory.TransactionHistoryOrderLines;
                        if (transactionLineItems.All(li => li.LineStatus == TransactionHistoryStatus.Settled.GetDescriptionAttribute()))
                        {
                            if (transactionHistory.Status != TransactionHistoryStatus.Settled.GetDescriptionAttribute())
                            {
                                transactionHistory.Status = TransactionHistoryStatus.Settled.GetDescriptionAttribute();
                                haveChange = true;

                                if (transactionHistory.TransactionType == TransactionHistoryType.DeliverFromVault.ToString())
                                {
                                    var customerId = new Guid(transactionHistory.CustomerRef);
                                    _bullionEmailHelper.SendBullionDispatchFromVaultEmail(customerId, BuildDispatchFromVaultOrderDto(customerId, transactionHistory));
                                }
                            }
                        }
                        else if (transactionLineItems.All(li => li.LineStatus != TransactionHistoryStatus.Inprogress.GetDescriptionAttribute()))
                        {
                            if (transactionHistory.Status != TransactionHistoryStatus.Pending.GetDescriptionAttribute())
                            {
                                transactionHistory.Status = TransactionHistoryStatus.Pending.GetDescriptionAttribute();
                                haveChange = true;
                            }
                        }

                        // Set IncludedInAXBalances based on rules
                        if (transactionHistory.Status != TransactionHistoryStatus.Inprogress.GetDescriptionAttribute()
                            && (transactionHistory.TransactionType == TransactionHistoryType.SellFromVault.ToString()
                                || transactionHistory.TransactionType == TransactionHistoryType.Purchase.ToString()))
                        {
                            transactionHistory.IncludedInAxBalances = true;
                            haveChange = true;
                        }
                        else if (transactionHistory.Status == TransactionHistoryStatus.Settled.GetDescriptionAttribute()
                                 && (transactionHistory.TransactionType == TransactionHistoryType.DeliverFromVault.ToString()
                                     || transactionHistory.TransactionType == TransactionHistoryType.FundsAddedCard.ToString()
                                     || transactionHistory.TransactionType == TransactionHistoryType.Withdraw.ToString()))
                        {
                            transactionHistory.IncludedInAxBalances = true;
                            haveChange = true;
                        }

                        if (haveChange) _transactionHistoryRepository.UpdateTransactionHistoryStatus(transactionHistory);

                        if (transactionHistory.TransactionType.Equals(TransactionHistoryType.Purchase.ToString()))
                        {
                            if (transactionLineItems.Where(x => x.LineDeliver == BullionDeliver.Vault.GetDescriptionAttribute())
                                .All(li => li.LineStatus == TransactionHistoryStatus.Settled.GetDescriptionAttribute()))
                            {
                                _walletRepository.UpdatePortfolioContentItemStatusToSettled(transactionHistory.OrderNumber);
                            }
                        }
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
        }

        public bool HasTransactionHistory(string customerId)
        {
            try
            {
                using (var db = new TransactionHistoriesContext())
                {
                    return db.TransactionHistory.Any(x => x.CustomerRef == customerId);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
        }

        public bool ShouldNotUpdateBalance(string customerId)
        {
            try
            {
                using (var db = new TransactionHistoriesContext())
                {
                    return db.TransactionHistory.Where(x => x.CustomerRef == customerId).Any(x => !x.IncludedInAxBalances);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
        }

        private DispatchFromVaultOrderDto BuildDispatchFromVaultOrderDto(Guid customerId, TransactionHistory transactionHistory)
        {
            var dispatchedItems =
                transactionHistory.TransactionHistoryOrderLines.Where(x => x.LineItemId.StartsWith("Li")).ToList();
            if (!dispatchedItems.Any()) return null;

            var firstDispatchedItem = dispatchedItems.FirstOrDefault();
            if (firstDispatchedItem == null) return null;
            var variantContent = firstDispatchedItem.VariantCode.GetVariantByCode();
            if (variantContent == null) return null;
            var currentContact = _customerContext.GetContactById(customerId);
            var shippingAddress = _bullionContactHelper.GetBullionAddress(currentContact);
            if (shippingAddress == null) return null;

            return new DispatchFromVaultOrderDto
            {
                OrderReference = transactionHistory.OrderNumber,
                ProductCode = firstDispatchedItem.VariantCode,
                ProductTitle = variantContent.DisplayName,
                ProductSubtitle = variantContent.SubDisplayName,
                Quantity = dispatchedItems.Sum(x => x.Quantity),
                OrderTotal = new Money(transactionHistory.Amount * -1, transactionHistory.CurrencyCode).ToString(),
                OrderDate = transactionHistory.CreatedDate.ToString("dd MMMM yyyy"),
                DeliveryAddress = new DeliveryAddressDto
                {
                    DeliveryAddressLine = new AddressDto
                    {
                        Name = shippingAddress.Name,
                        Line1 = shippingAddress.Line1,
                        Line2 = shippingAddress.Line2,
                        Line3 = shippingAddress.RegionName,
                        Line4 = string.Empty,
                        City = shippingAddress.City,
                        PostCode = shippingAddress.PostalCode,
                        CountryCode = shippingAddress.CountryCode
                    }
                },
                CustomerCode = currentContact.GetStringProperty(CustomFields.BullionObsAccountNumber),
                ProductImage = variantContent.GetDefaultAssetUrl().ToAbsoluteUrl()
            };
        }

        private BankDepositeModel BuildBankDepositModel(TransactionHistory transactionHistory, CustomerContactViewModel contact)
        {
            return new BankDepositeModel
            {
                EffectiveBalance = new Money(contact.EffectiveBalance, transactionHistory.CurrencyCode).ToString(),
                Date = transactionHistory.CreatedDate,
                DepositAmount = new Money(transactionHistory.Amount, transactionHistory.CurrencyCode).ToString()
            };
        }
        private TransactionHistory BuildPurchaseTransactionHistoryRecord(IPurchaseOrder purchaseOrder, CustomerContact customerContact, string clientIpAddress = "")
        {
            var currentContactId = customerContact != null ? (Guid)customerContact.PrimaryKeyId : _customerContext.CurrentContactId;
            var lineItemsToRecord = purchaseOrder.GetAllLineItems();

            var transactionHistoryRecord = new TransactionHistory(TransactionHistoryType.Purchase)
            {
                CurrencyCode = purchaseOrder.Currency.CurrencyCode,
                Status = TransactionHistoryStatus.Inprogress.GetDescriptionAttribute(),
                DeliveryFee = purchaseOrder.GetShippingTotal(),
                OrderNumber = purchaseOrder.OrderNumber,
                ExportTransactionId = purchaseOrder.Properties[CustomFields.ExportTransactionId].ToString(),
                ClientIpAddress = clientIpAddress
            };
            transactionHistoryRecord = transactionHistoryRecord
                .SetContactInfo(currentContactId, _userService.GetImpersonateUser(customerContact), purchaseOrder.Name)
                .SetAmountAndBalance(-purchaseOrder.GetTotal(), _bullionContactHelper.GetEffectiveBalance(customerContact ?? _customerContext.CurrentContact));

            var lineItemCount = 1;
            var curr = new Currency(purchaseOrder.Currency.CurrencyCode);

            foreach (var orderLine in lineItemsToRecord)
            {
                var vCost = orderLine.GetStringProperty(CustomFields.BullionVATCost);

                transactionHistoryRecord.TransactionHistoryOrderLines.Add(new TransactionHistoryOrderLine
                {
                    PkId = Guid.NewGuid(),
                    OrderNumber = purchaseOrder.OrderNumber,
                    LineItemId = $"Li-{purchaseOrder.OrderNumber}-{lineItemCount}",
                    Quantity = orderLine.Quantity,
                    VariantCode = orderLine.Code,
                    Total = orderLine.GetDiscountedPrice(purchaseOrder.Currency),
                    LineStatus = TransactionHistoryStatus.Inprogress.GetDescriptionAttribute(),
                    LineDeliver = orderLine.IsInVault() ? BullionDeliver.Vault.GetDescriptionAttribute() : BullionDeliver.Deliver.GetDescriptionAttribute(),
                    MetalPrice = orderLine.GetDecimalProperty(CustomFields.PampMetalPricePerUnitWithoutPremium),
                    VatCost = decimal.Parse(vCost, NumberStyles.Currency, curr.Format),
                    VatStatus = orderLine.GetStringProperty(CustomFields.BullionVATStatus),
                    DeliveryType = purchaseOrder.GetFirstShipment()?.ShippingMethodName,
                    ExVatLineTotalMetalPrice = orderLine.GetDecimalProperty(CustomFields.ExVatLineTotalMetalPrice),
                    ExVatLineTotalPremium = orderLine.GetDecimalProperty(CustomFields.BullionBuyPremiumCost),
                });
                lineItemCount++;
            }
            return transactionHistoryRecord;
        }

        #region Filter Helper Methods
        private string GetLocalizationKey(TransactionHistoryType type)
        {
            return _localizationService.GetStringByCulture(ConstructTranslationKeyForFilterOption(type), GetDefaultTextForFilterOption(type), ContentLanguage.PreferredCulture);
        }

        private string GetDefaultTextForFilterOption(TransactionHistoryType type)
        {
            switch (type)
            {
                case TransactionHistoryType.Purchase:
                    return StringConstants.TranslationFallback.TransactionHistoryPurchase;
                case TransactionHistoryType.FundsAddedCard:
                    return StringConstants.TranslationFallback.TransactionHistoryFundsAddedCard;
                case TransactionHistoryType.DeliverFromVault:
                    return StringConstants.TranslationFallback.TransactionHistoryDeliveryFromVault;
                case TransactionHistoryType.Withdraw:
                    return StringConstants.TranslationFallback.TransactionHistoryWithdraw;
                case TransactionHistoryType.SellFromVault:
                    return StringConstants.TranslationFallback.TransactionHistorySellFromVault;
                case TransactionHistoryType.StorageFee:
                    return StringConstants.TranslationFallback.TransactionHistoryStorageFee;
                case TransactionHistoryType.AccountActivity:
                    return StringConstants.TranslationFallback.TransactionHistoryAccountActitivy;
                case TransactionHistoryType.BalanceAdjustment:
                    return TransactionHistoryType.BalanceAdjustment.GetDescriptionAttribute();
                case TransactionHistoryType.BankDeposit:
                    return TransactionHistoryType.BankDeposit.GetDescriptionAttribute();
                default:
                    return string.Empty;
            }
        }

        private string ConstructTranslationKeyForFilterOption(TransactionHistoryType filterOption)
        {
            return string.Format(StringResources.TransactionHistoryFilterOption, filterOption.ToString());
        }
        #endregion

        #region Get Transactions Helper Methods

        private TransactionHistoryItemViewModel ConvertToViewModel(
            TransactionHistory record,
            Dictionary<TransactionHistoryType, string> transactionHistoryTypeTextMappings)
        {
            var viewModel = new TransactionHistoryItemViewModel(record, transactionHistoryTypeTextMappings);

            if (IsTransactionHavingOrderInformation(viewModel.TransactionType))
            {
                var orderLineRecords = record.TransactionHistoryOrderLines;
                if (!orderLineRecords.IsNullOrEmpty())
                {
                    var orderLineViewModels = orderLineRecords
                        .Select(x => ConvertToOrderLineModel(x, viewModel.TransactionType));
                        
                    viewModel.OrderDetailViewModel = new TransactionHistoryOrderDetailViewModel
                    {
                        OrderLineViewModels = orderLineViewModels,
                        //TODO: Get delivery type from transaction history order line
                        DeliveryType = string.Empty
                    };
                }
            }
            var detailBuilder = _transactionHistoryDetailBuilderHelpers.FirstOrDefault(x => x.IsSatified(viewModel.TransactionType));
            if (detailBuilder != null)
            {
                viewModel.MoreDetailCollection = detailBuilder.BuildTheDetailInformation(viewModel);
            }
            return viewModel;
        }
        
        private TransactionHistoryOrderLineItemViewModel ConvertToOrderLineModel(TransactionHistoryOrderLine orderLine, TransactionHistoryType type)
        {
            var currency = !string.IsNullOrEmpty(orderLine.TransactionHistory?.CurrencyCode) ? new Currency(orderLine.TransactionHistory.CurrencyCode) : Currency.GBP;
           
            var model = new TransactionHistoryOrderLineItemViewModel();
            var entry = _entryHelper.GetVariantFromCode(orderLine.VariantCode);

            if (entry != null)
            {
                model.IsSignatureVariant = entry is SignatureVariant;
                model.Name = entry.Name;
                model.Reference = entry.ContentLink;
                model.Code = entry.Code;
            }
            else
            {
                model.IsSignatureVariant = false;
                model.Name = orderLine.VariantCode;
                model.Reference = null;
                model.Code = orderLine.VariantCode;
            }

            model.Quantity = Math.Round(orderLine.Quantity);
            model.LineItemId = orderLine.LineItemId;
            model.Status = orderLine.LineStatus;
            model.DeliveryType = (!string.IsNullOrWhiteSpace(orderLine?.LineDeliver)) ? orderLine.LineDeliver : StringConstants.TranslationFallback.TransactionHistoryOrderDelivered; 

            if (type.Equals(TransactionHistoryType.SellFromVault))
            {
                model.QuantityInOz = orderLine.Quantity;
            }
            else
            {
                if (entry is IAmPremiumVariant premiumVariant)
                {
                    //TODO: Get weight value from transaction history order line, it cannot be calculated
                    model.QuantityInOz = Math.Round(orderLine.Quantity * premiumVariant.TroyOzWeightConfiguration, 3);

                    var coin = entry as CoinVariant;
                    if (coin?.CoinTubeQuantity.GetValueOrDefault(1) > 1)
                        model.QuantityInOz *= (decimal)coin?.CoinTubeQuantity;
                }
                else
                    model.QuantityInOz = model.Quantity;
            }

            model.VatCost = orderLine.VatCost != null
                ? new Money(orderLine.VatCost.Value, currency)
                : new Money?();
            model.VatStatus = orderLine.VatStatus;
            model.MetalSpotPrice = orderLine.ExVatLineTotalMetalPrice != null
                ? new Money(orderLine.ExVatLineTotalMetalPrice.Value / model.QuantityInOz, currency)
                : new Money?();
            model.ExVatLineTotalPremium = orderLine.ExVatLineTotalPremium != null
                ? new Money(orderLine.ExVatLineTotalPremium.Value, currency)
                : new Money?();

            // metal price might be null so just dividing 'Total' by 'QuantityInOz' here
            model.MetalPrice = new Money(orderLine.Total / model.QuantityInOz, currency);
            model.ProductPrice = new Money(orderLine.Total, currency);

            //TODO: Get VAT fee value from transaction history order line

            return model;
        }
      
        private bool IsTransactionHavingOrderInformation(TransactionHistoryType type)
        {
            return type == TransactionHistoryType.DeliverFromVault
                   || type == TransactionHistoryType.Purchase
                   || type == TransactionHistoryType.SellFromVault;
        }

        private Dictionary<TransactionHistoryType, string> GetTransactionTypeWithDescriptionMappings()
        {
            var result = new Dictionary<TransactionHistoryType, string>();
            var allTransactionTypes = Enum.GetValues(typeof(TransactionHistoryType)).Cast<TransactionHistoryType>();
            foreach (var type in allTransactionTypes)
            {
                result.Add(type, type.GetDescriptionAttribute());
            }
            return result;
        }

        private int GetSkip(int currentPageNumber, int pageSize)
        {
            return currentPageNumber >= 1 ? (currentPageNumber - 1) * pageSize : 0;
        }

        #endregion
    }
}