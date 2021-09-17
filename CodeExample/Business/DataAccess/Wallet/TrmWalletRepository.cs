using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using TrmWallet;
using TrmWallet.Entity;
using TRM.IntegrationServices.Constants;
using TRM.IntegrationServices.Models.Export;
using TRM.IntegrationServices.Models.Export.Payloads.DeliverFromVault;
using TRM.IntegrationServices.Models.Export.Payloads.SellFromVault;
using TRM.Shared.Extensions;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.BullionPortfolio;
using TRM.Web.Services;
using StringConstants = TRM.Web.Constants.StringConstants;
using TRM.IntegrationServices.Models.Impersonation;
using TRM.IntegrationServices.Interfaces;
using Mediachase.Commerce.Customers;
using TRM.Shared.Helpers;

namespace TRM.Web.Business.DataAccess.Wallet
{
    [ServiceConfiguration(typeof(ITrmWalletRepository), Lifecycle = ServiceInstanceScope.Transient)]
    public class TrmWalletRepository : DbContextDisposable<PortfolioDbContext>, ITrmWalletRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TrmWalletRepository));
        private readonly IExportTransactionService _exportTransactionService;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        private readonly IImpersonationLogService _impersonationLogService;
        private readonly IUserService _userService;

        public TrmWalletRepository(
            IExportTransactionService exportTransactionService,
            IAmBullionContactHelper bullionContactHelper,
            IUserService userService,
            IImpersonationLogService impersonationLogService)
        {
            _exportTransactionService = exportTransactionService;
            _bullionContactHelper = bullionContactHelper;
            _userService = userService;
            _impersonationLogService = impersonationLogService;
        }

        public IEnumerable<IWalletItem> GetCustomerWalletItems(Guid customerId, bool hasNoTracking = false)
        {
            var portfolioHeader = context.PortfolioHeaders.FirstOrDefault(x => x.CustomerId.Equals(customerId));

            if (portfolioHeader == null) return null;

            return hasNoTracking
                ? context.PortfolioContents.Where(x => x.PortfolioId.Equals(portfolioHeader.Id)).AsNoTracking()
                : context.PortfolioContents.Where(x => x.PortfolioId.Equals(portfolioHeader.Id));
        }

        public bool SaveTrmSellTransaction(ITrmSellTransaction trans)
        {
            using (var newDbContext = new PortfolioDbContext())
            {
                decimal actualQuantityToDeliverOrSell = 0;
                var walletItemsNeedToUpdate = GetWalletItemsCorrespondingToQuantityToDeliverOrSell(newDbContext,
                    trans.CustomerId, trans.VariantCode, trans.RequestedQuantityToSell,
                    out actualQuantityToDeliverOrSell);
                if (walletItemsNeedToUpdate == null || !walletItemsNeedToUpdate.Any() ||
                    trans.RequestedQuantityToSell != actualQuantityToDeliverOrSell)
                {
                    return false;
                }

                using (var transaction = newDbContext.Database.BeginTransaction())
                {
                    try
                    {
                        UpdateCustomerWalletItems(newDbContext, trans.WalletItems, walletItemsNeedToUpdate);

                        newDbContext.SaveChanges();

                        var isSuccess = SaveTrmSellTransactionItem(trans, walletItemsNeedToUpdate);
                        if (!isSuccess) throw new Exception("Can not create Sell Export Transaction");

                        transaction.Commit();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        //var entry = ex.Entries.Single();
                        //var clientValues = (PortfolioContent)entry.Entity;
                        //var dbValues = entry.GetDatabaseValues();
                        Logger.Error(ex.Message, ex);
                        transaction.Rollback();
                        return false;
                    }
                    catch (RetryLimitExceededException ex)
                    {
                        Logger.Error(ex.Message, ex);
                        transaction.Rollback();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message, ex);
                        transaction.Rollback();
                        return false;
                    }
                }
            }

            return true;
        }

        public bool SaveTrmDeliverTransaction(ITrmDeliverTransaction trans)
        {
            using (var newDbContext = new PortfolioDbContext())
            {
                decimal actualQuantityToDeliverOrSell = 0;
                var walletItemsNeedToUpdate = GetWalletItemsCorrespondingToQuantityToDeliverOrSell(newDbContext,
                    trans.CustomerId, trans.VariantCode, trans.QuantityToDeliver, out actualQuantityToDeliverOrSell);
                if (walletItemsNeedToUpdate == null || !walletItemsNeedToUpdate.Any() ||
                    trans.QuantityToDeliver != actualQuantityToDeliverOrSell)
                {
                    return false;
                }

                using (var transaction = newDbContext.Database.BeginTransaction())
                {
                    try
                    {
                        UpdateCustomerWalletItems(newDbContext, trans.WalletItems, walletItemsNeedToUpdate);

                        newDbContext.SaveChanges();

                        var isSuccess = SaveTrmDeliverTransactionItem(trans, walletItemsNeedToUpdate);
                        if (!isSuccess) throw new Exception("Can not create Deliver Export Transaction");

                        transaction.Commit();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        //var entry = ex.Entries.Single();
                        //var clientValues = (PortfolioContent)entry.Entity;
                        //var dbValues = entry.GetDatabaseValues();
                        Logger.Error(ex.Message, ex);
                        transaction.Rollback();
                        return false;
                    }
                    catch (RetryLimitExceededException ex)
                    {
                        Logger.Error(ex.Message, ex);
                        transaction.Rollback();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message, ex);
                        transaction.Rollback();
                        return false;
                    }
                }
            }

            return true;
        }

        public bool CreatePortfolioContentsWhenPurchase(Guid customerId, IEnumerable<IWalletItem> portfolioContents)
        {
            using (var newDbContext = new PortfolioDbContext())
            {
                var portfolioHeader =
                    newDbContext.PortfolioHeaders.FirstOrDefault(x => x.CustomerId.Equals(customerId));

                if (portfolioHeader != null)
                {
                    //Update portfolio header
                    portfolioHeader.UpdatedByEpi = DateTime.UtcNow;
                }
                else
                {
                    portfolioHeader = new PortfolioHeader
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerId,
                        UpdatedByEpi = DateTime.Now
                    };
                }

                return UpdatePortfolio(newDbContext, portfolioHeader, portfolioContents);
            }
        }

        public bool ImportPortfolioFromAX(ITrmCustomerWallet customerWallet, IEnumerable<IWalletItem> portfolioContents)
        {
            using (var newDbContext = new PortfolioDbContext())
            {
                var portfolioHeader =
                    newDbContext.PortfolioHeaders.FirstOrDefault(x => x.CustomerId.Equals(customerWallet.CustomerId));

                if (portfolioHeader != null)
                {
                    //Update portfolio header
                    portfolioHeader.LastUpdatedFromAX = customerWallet.LastUpdatedFromAX;

                    var dbPortfolioContents = newDbContext.PortfolioContents
                        .Where(x => x.PortfolioId == portfolioHeader.Id).ToList();
                    //delete all existed portfolio contents
                    newDbContext.PortfolioContents.RemoveRange(dbPortfolioContents);
                    newDbContext.SaveChanges();
                }
                else
                {
                    portfolioHeader = (PortfolioHeader) customerWallet;
                }

                return UpdatePortfolio(newDbContext, portfolioHeader, portfolioContents);
            }
        }

        public bool UpdatePortfolioContentItemStatusToSettled(string orderNumber)
        {
            using (var newDbContext = new PortfolioDbContext())
            {
                using (var transaction = newDbContext.Database.BeginTransaction())
                {
                    try
                    {
                        var portfolioContents =
                            newDbContext.PortfolioContents.Where(x => x.OriginalPurchaseOrderNumber == orderNumber);

                        foreach (var portfolioContent in portfolioContents)
                        {
                            portfolioContent.Status = StringConstants.PortfolioContentStatus.Settled;
                            newDbContext.PortfolioContents.AddOrUpdate(portfolioContent);
                        }

                        newDbContext.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message, ex);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        private bool UpdatePortfolio(PortfolioDbContext newDbContext, ITrmCustomerWallet customerWallet,
            IEnumerable<IWalletItem> portfolioContents)
        {
            using (var transaction = newDbContext.Database.BeginTransaction())
            {
                try
                {
                    newDbContext.PortfolioHeaders.AddOrUpdate(customerWallet as PortfolioHeader);

                    //Update portfolio contents
                    foreach (var portfolioContent in portfolioContents)
                    {
                        portfolioContent.PortfolioId = customerWallet.Id;
                        newDbContext.PortfolioContents.AddOrUpdate((PortfolioContent) portfolioContent);
                    }

                    newDbContext.SaveChanges();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message, ex);
                    transaction.Rollback();
                    return false;
                }
            }

            return true;
        }

        private void UpdateCustomerWalletItems(PortfolioDbContext dbContext,
            IEnumerable<IWalletItem> originalWalletItems, IEnumerable<IWalletItem> walletItems)
        {
            foreach (var item in walletItems)
            {
                dbContext.PortfolioContents.AddOrUpdate((PortfolioContent) item);

                var originalRowVersion = originalWalletItems.FirstOrDefault(x => x.Id.Equals(item.Id))?.RowVersion;
                dbContext.Entry(item).OriginalValues["RowVersion"] = originalRowVersion;
            }
        }

        private IEnumerable<IWalletItem> GetWalletItemsCorrespondingToQuantityToDeliverOrSell(
            PortfolioDbContext dbContext, Guid customerId, string variantCode, decimal quantity,
            out decimal actualQuantityToDeliverOrSell)
        {
            var resultItems = new List<IWalletItem>();
            var desireQuantity = quantity;
            actualQuantityToDeliverOrSell = 0;

            var portfolioHeader = dbContext.PortfolioHeaders.FirstOrDefault(x => x.CustomerId.Equals(customerId));

            if (portfolioHeader == null) return resultItems;

            var customerWalletItems = dbContext.PortfolioContents.Where(x => x.PortfolioId.Equals(portfolioHeader.Id));

            var variant = variantCode?.GetVariantByCode();
            var premiumVariant = variant as IAmPremiumVariant;
            if (variant == null || premiumVariant == null) return resultItems;

            var variantType = variant.GetBullionVariantType();

            var stackItems = customerWalletItems
                .Where(x => x.VariantCode.Equals(variantCode) &&
                            x.QuantityInVault > 0 &&
                            x.Status.Equals(StringConstants.PortfolioContentStatus.Settled))
                .OrderBy(x => x.PurchaseDate)
                .ToList();

            foreach (var stackItem in stackItems)
            {
                if (quantity == 0) break;

                if (variantType.Equals(Enums.BullionVariantType.Signature))
                {
                    if (0 < stackItem.WeightInVault && stackItem.WeightInVault <= quantity)
                    {
                        quantity = quantity - stackItem.WeightInVault;
                        stackItem.WeightInVault = 0;
                        resultItems.Add(stackItem);
                        continue;
                    }

                    if (stackItem.WeightInVault > quantity)
                    {
                        stackItem.WeightInVault = stackItem.WeightInVault - quantity;
                        quantity = 0;
                        resultItems.Add(stackItem);
                    }
                }
                else
                {
                    if (0 < stackItem.QuantityInVault && stackItem.QuantityInVault <= quantity)
                    {
                        quantity = quantity - stackItem.QuantityInVault;
                        stackItem.QuantityInVault = 0;
                        stackItem.WeightInVault = 0;
                        resultItems.Add(stackItem);
                        continue;
                    }

                    if (stackItem.QuantityInVault > quantity)
                    {
                        stackItem.QuantityInVault = stackItem.QuantityInVault - quantity;
                        stackItem.WeightInVault = stackItem.QuantityInVault * premiumVariant.TroyOzWeight;
                        quantity = 0;
                        resultItems.Add(stackItem);
                    }
                }
            }

            actualQuantityToDeliverOrSell = desireQuantity - quantity;
            return resultItems;
        }

        private bool SaveTrmSellTransactionItem(ITrmSellTransaction trans, IEnumerable<IWalletItem> walletItems)
        {
            trans.CreatedDate = DateTime.UtcNow;
            trans.LastUpdateDate = DateTime.UtcNow;

            ExportTransaction sellExportTransaction;

            var isSuccess = _exportTransactionService.CreateExportTransaction(
                CreateSellFromVaultPayload(trans, walletItems),
                trans.CustomerId.ToString(),
                ExportTransactionType.SellFromVault,
                out sellExportTransaction);

            if (isSuccess && sellExportTransaction != null)
            {
                UserDetails userDetails;
                if (_userService.IsImpersonating())
                    userDetails = UserDetails.ForImpersonator(CustomerContext.Current, RequestHelper.GetClientIpAddress());
                else
                    userDetails = UserDetails.ForCustomer(CustomerContext.Current, RequestHelper.GetClientIpAddress());

                var impersonationLog = ImpersonationLog.ForExportTransaction(
                    userDetails,
                    sellExportTransaction,
                    $"{trans.TransactionNumberPrefix}{trans.TransactionNumber}");
                _impersonationLogService.CreateLog(impersonationLog);

                trans.TransactionNumber = sellExportTransaction.TransactionNumber.ToString();
                trans.ExportTransactionId = sellExportTransaction.TransactionId;
                _exportTransactionService.UpdateExportTransaction(sellExportTransaction.TransactionId,
                    CreateSellFromVaultPayload(trans, walletItems),
                    trans.CustomerId.ToString(),
                    ExportTransactionType.SellFromVault);
            }

            return isSuccess;
        }

        private SellFromVaultTransactionPayload CreateSellFromVaultPayload(ITrmSellTransaction trans,
            IEnumerable<IWalletItem> walletItems)
        {
            var customer = (trans as TrmSellTransaction)?.Customer;
            var premiumVariant = trans.VariantCode.GetVariantByCode() as IAmPremiumVariant;
            if (premiumVariant == null)
                throw new ArgumentNullException(trans.VariantCode, "The variant must be IAmPremiumVariant");

            return new SellFromVaultTransactionPayload
            {
                SellTransaction = new SellFromVaultExportTransaction
                {
                    Header = new SellFromVaultHeader
                    {
                        CurrencyCode = trans.Currency,
                        OrderRef = $"{trans.TransactionNumberPrefix}{trans.TransactionNumber}",
                        MaginusCustomerCode =
                            customer.GetStringProperty(Shared.Constants.StringConstants.CustomFields
                                .BullionObsAccountNumber),
                        HeaderCustomField = new SellFromVaultHeaderCustomField
                        {
                            ShipComplete = "false",
                            TransactionDateTime = trans.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            PampTradeId = trans.PampQuoteId
                        }
                    },
                    Lines = new List<SellFromVaultLine>
                    {
                        new SellFromVaultLine
                        {
                            LineItemId = $"Li-{trans.TransactionNumberPrefix}{trans.TransactionNumber}-1",
                            ProductCode = trans.VariantCode,
                            Quantity = premiumVariant.ConvertEpiQuantityToAxQuantity(trans.RequestedQuantityToSell)
                                .ToString("0.###"),
                            Price = ((trans.MetalPrice - trans.SellPremium) /
                                     premiumVariant.ConvertEpiQuantityToAxQuantity(trans.RequestedQuantityToSell))
                                .ToString("0.####"),
                            MetalPrice = trans.MetalPrice.ToString("0.##"),
                            LineExVatPAMPMetalValue = trans.MetalPrice.ToString("0.##"),
                            LineExVatPremiumCost = trans.SellPremium.ToString("0.##"),
                            LineTotal = (trans.MetalPrice - trans.SellPremium).ToString("0.##"),
                            UnitMetalWeightOz = premiumVariant.TroyOzWeightConfiguration.ToString("0.###"),
                            OriginalPurchaseOrderRefs =
                                walletItems.Select(x => x.OriginalPurchaseOrderNumber).ToArray(),
                            EpiTransIds = walletItems.Select(x => x.EpiTransId).ToArray()
                        }
                    }
                }
            };
        }

        private bool SaveTrmDeliverTransactionItem(ITrmDeliverTransaction trans, IEnumerable<IWalletItem> walletItems)
        {
            trans.CreatedDate = DateTime.UtcNow;
            trans.LastUpdateDate = DateTime.UtcNow;
            trans.OriginalPurchaseOrders = GetOriginalPurchaseOrder(trans, walletItems);

            ExportTransaction deliverExportTransaction;
            var isSuccess = _exportTransactionService.CreateExportTransaction(CreateDeliverPayload(trans),
                trans.CustomerId.ToString(),
                ExportTransactionType.DeliverFromVault,
                out deliverExportTransaction);

            if (isSuccess && deliverExportTransaction != null)
            {
                UserDetails userDetails;
                if (_userService.IsImpersonating())
                    userDetails = UserDetails.ForImpersonator(CustomerContext.Current, RequestHelper.GetClientIpAddress());
                else
                    userDetails = UserDetails.ForCustomer(CustomerContext.Current, RequestHelper.GetClientIpAddress());

                var impersonationLog = ImpersonationLog.ForExportTransaction(
                    userDetails,
                    deliverExportTransaction,
                    $"{trans.TransactionNumberPrefix}{trans.TransactionNumber}");
                _impersonationLogService.CreateLog(impersonationLog);

                trans.TransactionNumber = deliverExportTransaction.TransactionNumber.ToString();
                trans.ExportTransactionId = deliverExportTransaction.TransactionId;
                _exportTransactionService.UpdateExportTransaction(deliverExportTransaction.TransactionId,
                    CreateDeliverPayload(trans),
                    trans.CustomerId.ToString(),
                    ExportTransactionType.SellFromVault); // is this correct?
            }

            return isSuccess;
        }

        private IEnumerable<IOriginalPurchaseOrder> GetOriginalPurchaseOrder(ITrmDeliverTransaction trans,
            IEnumerable<IWalletItem> walletItems)
        {
            var originalPurchaseOrders = new List<OriginalPurchaseOrder>();
            foreach (var walletItem in walletItems)
            {
                var matchItem = trans.WalletItems.FirstOrDefault(x => x.Id.Equals(walletItem.Id));
                if (matchItem != null)
                {
                    originalPurchaseOrders.Add(new OriginalPurchaseOrder
                    {
                        PortfolioContentId = walletItem.Id,
                        EpiTransId = walletItem.EpiTransId,
                        QuantityToDeliver = matchItem.QuantityToDeliver,
                        VATCode = trans.VatCode,
                        VATToPay = walletItem.OriginalUnitPrice * matchItem.QuantityToDeliver * trans.VatRate / 100,
                        OriginalPurchaseOrderNumber = walletItem.OriginalPurchaseOrderNumber
                    });
                }
            }

            return originalPurchaseOrders;
        }

        private DeliverFromVaultTransactionPayload CreateDeliverPayload(ITrmDeliverTransaction trans)
        {
            var customer = (trans as TrmDeliverTransaction)?.Customer;
            if (customer != null)
            {
                var customerAddress = _bullionContactHelper.GetBullionAddress(customer);
                var index = 1;
                return new DeliverFromVaultTransactionPayload
                {
                    DeliverTransaction = new DeliverFromVaultExportTransaction
                    {
                        Header = new DeliverFromVaultHeader
                        {
                            CurrencyCode = trans.Currency,
                            OrderRef = $"{trans.TransactionNumberPrefix}{trans.TransactionNumber}",
                            MaginusCustomerCode = customer?.GetStringProperty(Shared.Constants.StringConstants
                                .CustomFields.BullionObsAccountNumber),
                            PromotionCode = string.Empty,
                            DeliveryAddressLine = new DeliverFromVaultAddress
                            {
                                RecepientName = _bullionContactHelper.GetFullname(customer),
                                AddressLine1 = string.IsNullOrEmpty(customerAddress?.Line1)
                                    ? string.Empty
                                    : customerAddress?.Line1,
                                AddressLine2 = string.IsNullOrEmpty(customerAddress?.Line2)
                                    ? string.Empty
                                    : customerAddress?.Line2,
                                AddressLine3 = string.IsNullOrEmpty(customerAddress?.City)
                                    ? string.Empty
                                    : customerAddress?.City,
                                AddressLine5 = string.IsNullOrEmpty(customerAddress?.State)
                                    ? string.Empty
                                    : customerAddress?.State,
                                PostCode = string.IsNullOrEmpty(customerAddress?.PostalCode)
                                    ? string.Empty
                                    : customerAddress?.PostalCode,
                                CountryCode = string.IsNullOrEmpty(customerAddress?.CountryCode)
                                    ? string.Empty
                                    : customerAddress?.CountryCode
                            },
                            HeaderCustomField = new DeliverFromVaultHeaderCustomField
                            {
                                ShipComplete = "false",
                                TransactionDateTime = trans.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                            }
                        },
                        Lines = trans.OriginalPurchaseOrders.Select(x => new DeliverFromVaultLine
                        {
                            EpiTransId = x.EpiTransId,
                            LineItemId = $"Li-{trans.TransactionNumberPrefix}{trans.TransactionNumber}-{index++}",
                            ProductCode = trans.VariantCode,
                            CarriageCode = trans.CarriageCode,
                            Quantity = trans.VariantCode.ParseEpiQtyToAx(x.QuantityToDeliver).ToString("0.###"),
                            Price = x.VATToPay.ToString("0.##"),
                            LineVatCost = decimal.Zero.ToString("0.##"),
                            OriginalPurchaseOrderRef = x.OriginalPurchaseOrderNumber,
                            PromiseDate = string.Empty,
                            PromotionCode = string.Empty,
                            DateRequired = trans.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                        }).ToList(),
                        Charges = new List<DeliverFromVaultCharge>
                        {
                            new DeliverFromVaultCharge
                            {
                                ChargeId = $"Ch-{trans.TransactionNumberPrefix}{trans.TransactionNumber}-1",
                                ChargeCode = "Delivery",
                                ChargeValue = (trans.ShippingFee + trans.ShippingFeeVAT).ToString("0.##"),
                                ChargeValueExVat = trans.ShippingFee.ToString("0.##")
                            }
                        },
                        Payments = new List<DeliverFromVaultPayment>
                        {
                            new DeliverFromVaultPayment
                            {
                                PaymentValue = trans.TotalAmount.ToString("0.##"),
                                PaymentType = "1",
                                CardTypeId = "Wallet"
                            }
                        }
                    }
                };
            }

            return new DeliverFromVaultTransactionPayload();
        }
    }
}
