using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using EPiServer.Logging;
using Hephaestus.ContentTypes.Business.Extensions;
using Mediachase.Commerce;
using TRM.Shared.Extensions;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Facades;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DDS;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.InvoiceStatements;
using TRM.Web.Models.ViewModels;
using TRM.Web.Models.ViewModels.Bullion.Report;
using TRM.Web.Utils;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Services.InvoiceStatements
{
    public class BullionStatementService : DbContextDisposable<BullionInvoiceStatementDbContext>, IBullionStatementService
    {
        private readonly IList<string> _fixedMetalOrder = new List<string>
        {
            PricingAndTradingService.Models.Constants.MetalType.Gold.ToString(),
            PricingAndTradingService.Models.Constants.MetalType.Silver.ToString(),
            PricingAndTradingService.Models.Constants.MetalType.Platinum.ToString()
        };

        private readonly IList<string> _fixedCategoryOrder = new List<string>
        {
            Enums.BullionVariantType.Coin.ToString(),
            Enums.BullionVariantType.Bar.ToString(),
            Enums.BullionVariantType.Signature.ToString()
        };

        private readonly IList<string> _fixedTransactionOrder = new List<string>
        {
            "Buys", "Sells", "Deposits", "Withdrawals", "Storage and Management Fees", "Deliver From Vault Fees", "Miscellaneous"
        };

        private readonly CustomerContextFacade _customerContext;
        private readonly IBullionDocumentRepository _documentRepository;
        private readonly IMetalPriceService _metalPriceService;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(BullionStatementService));
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionStatementService(CustomerContextFacade customerContext, IBullionDocumentRepository documentRepository, IMetalPriceService metalPriceService, IAmBullionContactHelper bullionContactHelper)
        {
            _customerContext = customerContext;
            _documentRepository = documentRepository;
            _metalPriceService = metalPriceService;
            _bullionContactHelper = bullionContactHelper;
        }

        public List<DocumentDto> GetBullionDocumentList(Guid customerId, int year)
        {
            return _documentRepository.GetDocumentsByCustomerId(customerId, year)
                .OrderByDescending(x => x.Date)
                .ToList()
                .Select(x => new DocumentDto
                {
                    Created = x.Date.ToString("dd MMMM yyyy"),
                    FromDate = x.FromDate.ToString("dd MMMM yyyy"),
                    ToDate = x.ToDate.ToString("dd MMMM yyyy"),
                    Type = x.Type,
                    Month = x.Date.ToString("MMMM"),
                    ID = x.Id,
                    DownloadLink = x.PdfPath
                }).ToList();
        }

        public List<int> GetBullionDocumentYearList(Guid customerId)
        {
            return _documentRepository.GetDocumentsByCustomerId(customerId)
                .GroupBy(x => x.Date.Year)
                .OrderByDescending(x => x.Key)
                .Select(x => x.Key).ToList();
        }

        public string GenerateStatementDetailReport(Guid statementId)
        {
            return MvcUtil.RenderPartialViewToString("StatementPdfTemplate", BuildBullionStatementViewModel(statementId));
        }

        public bool AddCustomerStatementFromImportData(AxImportData.CustomerStmtsStatement statement, Guid customerId)
        {
            var statementHeader = new StatementHeader
            {
                CustomerId = customerId,
                OpeningBalance = statement.OpeningBalance.ToDecimalExactCulture(),
                ClosingBalance = statement.ClosingBalance.ToDecimalExactCulture()
            };
            
            var statementTransactions = statement.Transactions.Select(x => new StatementTransaction
            {
                Id = Guid.NewGuid(),
                Date = x.TransDate.ToSqlDatetime(),
                Type = Constants.StringConstants.AxTransactionType.ConvertToEpiTransactionType(x.Type),
                Description = x.Description,
                ReferenceNumber = x.EpiTransId.GetOrderNumberFromEpiTransId(),
                Status = x.Status,
                Amount = x.Amount.ToDecimalExactCulture(),
                EffectiveBalance = x.EffectiveBalance.ToDecimalExactCulture(),
                VariantCode = x.VariantCode,
                SubmittedBy = x.SubmittedBy,
                WithdrawalsBankAccount = x.BankAccount
            }).ToList();

            var variantCodeDictionary = statement.Items.Select(x => x.ItemId).Distinct()
                .Select(x => x.GetVariantByCode() as IAmPremiumVariant)
                .Where(x => x != null)
                .ToDictionary(x => x.Code, y => y);

            var statementVaultItems = statement.Items.Select(x => new StatementVaultItem
            {
                Id = Guid.NewGuid(),
                OrderId = x.EpiTransId.GetOrderNumberFromEpiTransId(),
                OrderLineId = x.EpiTransId,
                VariantCode = x.ItemId,
                QuantityInVault = variantCodeDictionary.TryGet(x.ItemId)?.ParseAxQuantityToEpi(x.Qty.ToDecimalExactCulture()) ?? 0,
                WeightInVault = variantCodeDictionary.TryGet(x.ItemId)?.TroyOzWeightConfiguration * x.Qty.ToDecimalExactCulture() ?? 0,
                Status = x.Status
            });

            var statementDate = statement.Generated.ToSqlDatetime();
            var statementDocument = new Document
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                CurrencyCode = "",
                ToDate = statement.ToDate.ToSqlDatetime(),
                FromDate = statement.FromDate.ToSqlDatetime(),
                Date = statement.Generated.ToSqlDatetime(),
                Type = Enums.BullionDocumentType.Statement.ToString(),
                StatementHeader = statementHeader,
                StatementTransactions = statementTransactions.ToList(),
                StatementVaultItems = statementVaultItems.ToList(),
                Status = BullionDocumentStatus.DataNoPdf
            };

            if (statementTransactions.Any())
            {
                var latestDate = statementTransactions.Max(x => x.Date);
                statementDocument.StatementHolding = new StatementHolding
                {
                    EffectiveBalance = statementTransactions.LastOrDefault(x => x.Date == latestDate)?.EffectiveBalance ?? decimal.Zero
                };
            }

            // Check if historic metal price is stored
            _logger.Error($"Start Check if historic metal price is stored - Before - IsIndicativeMetalPricesStoredForStatement" );
            if (!_documentRepository.IsIndicativeMetalPricesStoredForStatement(statementDate))
            {
                statementDocument.StatementIndicativePrices = null; //GetStatementIndicativePricesByDate(statementDate);
            }

            _documentRepository.ImportDocumentFromAx(statementDocument);
            _logger.Error($"Document successfully imported");
            return true;
        }

        private List<StatementIndicativePrice> GetStatementIndicativePricesByDate(DateTime date)
        {
            var indicativePricesList = _metalPriceService.GetIndicativePriceByDate(date);
            if (indicativePricesList != null && indicativePricesList.Any())
            {
                var statementIndicativePrices = new List<StatementIndicativePrice>();
                foreach (var indicativePrices in indicativePricesList)
                {
                    statementIndicativePrices.Add(new StatementIndicativePrice
                    {
                        Id = Guid.NewGuid(),
                        Metal = PricingAndTradingService.Models.Constants.MetalType.Gold.ToString(),
                        Currency = indicativePrices.Currency,
                        SellPrice = indicativePrices.GoldPrice,
                        StatementDate = date
                    });

                    statementIndicativePrices.Add(new StatementIndicativePrice
                    {
                        Id = Guid.NewGuid(),
                        Metal = PricingAndTradingService.Models.Constants.MetalType.Silver.ToString(),
                        Currency = indicativePrices.Currency,
                        SellPrice = indicativePrices.SilverPrice,
                        StatementDate = date
                    });

                    statementIndicativePrices.Add(new StatementIndicativePrice
                    {
                        Id = Guid.NewGuid(),
                        Metal = PricingAndTradingService.Models.Constants.MetalType.Platinum.ToString(),
                        Currency = indicativePrices.Currency,
                        SellPrice = indicativePrices.PlatinumPrice,
                        StatementDate = date
                    });
                }
                _logger.Error($"Exit from GetStatementIndicativePricesByDate without error");

                return statementIndicativePrices;
            }

            return null;
        }

        //Get document info
        private BullionStatementViewModel BuildBullionStatementViewModel(Guid statementId)
        {
            var statement = _documentRepository.GetStatementById(statementId);
            PopulateStatementDependencies(statement);

            var summary = GetStatementSummary(statement);
            var metalPrices = GetBullionStatementMetalPrices(statement);
            var financialBreakdown = GetFinancialBreakdown(statement);
            var vaultByMetal = GetVaultedByMetal(statement);
            var vaultByCategory = GetVaultedByCategory(statement);
            var vaultBySubCategory = GetVaultedBySubCategory(statement);
            var vaultByRange = GetVaultedByRange(statement);
            var vaultByProduct = GetVaultedByProduct(statement);
            var periodActivity = GetPeriodActivity(statement);
            var totalAssets = GetTotalAssets(statement, vaultByProduct);

            return new BullionStatementViewModel(
                summary,
                metalPrices,
                totalAssets,
                financialBreakdown,
                vaultByMetal,
                vaultByCategory,
                vaultBySubCategory,
                vaultByRange,
                vaultByProduct,
                periodActivity);
        }

        private void PopulateStatementDependencies(Document statement)
        {
            if (statement == null) return;

            foreach (var vaultItem in statement.StatementVaultItems)
            {
                var variant = vaultItem.VariantCode.GetVariantByCode();
                vaultItem.Variant = variant;
                vaultItem.BullionType = variant.GetBullionVariantType();
                vaultItem.MetalType = (variant as IAmPremiumVariant)?.MetalType.ToString();
                vaultItem.Range = GetVariantRange(variant);
            }

            foreach (var transaction in statement.StatementTransactions)
            {
                if (string.IsNullOrEmpty(transaction.VariantCode)) continue;
                var variant = transaction.VariantCode.GetVariantByCode();
                transaction.Variant = variant;
            }

            var customer = _customerContext.GetContactById(statement.CustomerId);
            statement.CurrencyCode = _bullionContactHelper.GetDefaultCurrencyCode(customer);

            statement.MetalPriceDictionary = statement.StatementIndicativePrices
                .Where(x => x.Currency.Equals(statement.CurrencyCode))
                .GroupBy(x => x.Metal)
                .ToDictionary(x => x.Key, x => x.FirstOrDefault().SellPrice);
        }

        private static DynamicDataStore CoinRangeStore => typeof(CoinRange).GetStore();

        private CoinRange GetVariantRange(TrmVariant variant)
        {
            var coinVariant = variant as CoinVariant;
            if (coinVariant != null)
            {
                return CoinRangeStore.Items<CoinRange>().FirstOrDefault(x => x.Value.Equals(coinVariant.CoinRange));
            }

            var barVariant = variant as BarVariant;
            if (barVariant != null)
            {
                return new CoinRange
                {
                    DisplayName = "Bars",
                    Value = "Bars",
                    Rank = int.MaxValue - 1
                };
            }

            var signatureVariant = variant as SignatureVariant;
            if (signatureVariant != null)
            {
                return new CoinRange
                {
                    DisplayName = "Signature",
                    Value = "Signature",
                    Rank = int.MaxValue
                };
            }

            return null;
        }

        //Get Customer info
        private BullionStatementSummary GetStatementSummary(Document statement)
        {
            var customerContact = _customerContext.GetContactById(statement.CustomerId);
            var customerAddress = _bullionContactHelper.GetBullionAddress(customerContact);
            return new BullionStatementSummary
            {
                CustomerName = _bullionContactHelper.GetFullname(customerContact),
                AddressLine1 = customerAddress?.Line1,
                AddressLine2 = customerAddress?.Line2,
                Postcode = customerAddress?.PostalCode,
                Country = customerAddress?.CountryName,
                Account = customerContact?.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber),
                Period = $"{statement.FromDate.ToCommonUKFormat()} - {statement.ToDate.ToCommonUKFormat()}"
            };
        }

        //Get Metal prices
        private BullionStatementMetalPrices GetBullionStatementMetalPrices(Document statement)
        {
            if (statement?.StatementIndicativePrices == null || (statement.StatementIndicativePrices!= null && statement.StatementIndicativePrices.Count == 0)) return new BullionStatementMetalPrices();

            var metalPriceDic = statement.MetalPriceDictionary;

            return new BullionStatementMetalPrices
            {
                Gold = new Money(metalPriceDic[PricingAndTradingService.Models.Constants.MetalType.Gold.ToString()], statement.CurrencyCode).ToString(),
                Silver = new Money(metalPriceDic[PricingAndTradingService.Models.Constants.MetalType.Silver.ToString()], statement.CurrencyCode).ToString(),
                Platinum = new Money(metalPriceDic[PricingAndTradingService.Models.Constants.MetalType.Platinum.ToString()], statement.CurrencyCode).ToString()
            };
        }

        //Get Total assets
        private TotalAssets GetTotalAssets(Document statement, VaultedSection vaultByProduct)
        {
            var balance = new Money(statement.StatementHolding.EffectiveBalance, statement.CurrencyCode);
            var portfolio = new Money(vaultByProduct.Items.Sum(x => x.AmountMoney), statement.CurrencyCode);
            return new TotalAssets
            {
                Header = $"Total Asset Value at {statement.ToDate.ToCommonUKFormat()}",
                Balance = balance.ToString(),
                Portfolio = portfolio.ToString(),
                Total = new Money(balance + portfolio, statement.CurrencyCode).ToString()
            };
        }

        //Build Financial Breakdown
        private FinancialBreakdown GetFinancialBreakdown(Document statement)
        {
            var items = statement.StatementTransactions
                .GroupBy(x => GetFinancialBreakdownName(x.Type))
                .Select(x => new FinancialBreakdownItem
                {
                    Name = x.Key,
                    Total = new Money(x.Sum(y => y.Amount), statement.CurrencyCode).ToString()
                }).ToList();

            foreach (var transactionName in _fixedTransactionOrder)
            {
                var item = items.FirstOrDefault(x => x.Name.Equals(transactionName));
                if (item != null) continue;

                items.Add(new FinancialBreakdownItem
                {
                    Name = transactionName,
                    Total = new Money(0, statement.CurrencyCode).ToString()
                });
            }

            return new FinancialBreakdown
            {
                StartBalance = new Money(statement.StatementHeader.OpeningBalance, statement.CurrencyCode).ToString(),
                Items = items.OrderBy(x => _fixedTransactionOrder.IndexOf(x.Name)).ToArray(),
                EndBalance = new Money(statement.StatementHeader.ClosingBalance, statement.CurrencyCode).ToString()
            };
        }

        private string GetFinancialBreakdownName(string transactionKey)
        {
            if (transactionKey.Equals(Enums.StatementActivityType.Buy.ToString()))
            {
                return "Buys";
            }

            if (transactionKey.Equals(Enums.StatementActivityType.Deposit.ToString()))
            {
                return "Deposits";
            }

            if (transactionKey.Equals(Enums.StatementActivityType.Sell.ToString()))
            {
                return "Sells";
            }

            if (transactionKey.Equals(Enums.StatementActivityType.Withdrawal.ToString()))
            {
                return "Withdrawals";
            }

            if (transactionKey.Equals(Enums.StatementActivityType.DeliverFromVault.ToString()))
            {
                return "Deliver From Vault Fees";
            }

            if (transactionKey.Equals(Enums.StatementActivityType.StorageFee.ToString()) ||
                transactionKey.Equals(Enums.StatementActivityType.ManagementFee.ToString()))
            {
                return "Storage and Management Fees";
            }

            return "Miscellaneous";
        }

        //Build Vaulted group by Metal
        private VaultedSection GetVaultedByMetal(Document statement)
        {
            var metalPriceDic = statement.MetalPriceDictionary;

            var items = statement.StatementVaultItems
                .Where(x => !string.IsNullOrEmpty(x.MetalType))
                .GroupBy(x => x.MetalType)
                .OrderBy(x => _fixedMetalOrder.IndexOf(x.Key))
                .Select(x => new VaultedItem
                {
                    Name = x.Key,
                    Quantity = x.Sum(y => y.WeightInVault).ToString("0.###"),
                    AmountMoney = new Money(x.Sum(y => y.WeightInVault) * metalPriceDic.TryGet(x.Key.ToString()), statement.CurrencyCode)
                })
                .ToArray();

            return new VaultedSection
            {
                Header = "Vaulted Portfolio by Metal Split",
                Total = new Money(items.Sum(x => x.AmountMoney), statement.CurrencyCode).ToString(),
                Items = items
            };
        }

        //Build Vaulted group by Category
        private VaultedSection GetVaultedByCategory(Document statement)
        {
            var items = statement.StatementVaultItems
                .GroupBy(x => x.BullionType)
                .Where(x => !x.Key.Equals(Enums.BullionVariantType.None))
                .OrderBy(x => _fixedCategoryOrder.IndexOf(x.Key.ToString()))
                .Select(x => new VaultedItem
                {
                    Name = x.Key.Equals(Enums.BullionVariantType.Signature) ? $"{x.Key}" : $"{x.Key}s",
                    Quantity = x.Sum(y => y.WeightInVault).ToString("0.###"),
                    AmountMoney = new Money(x.Sum(y => CalculateEffectiveValue(statement, y)), statement.CurrencyCode)
                })
                .ToArray();

            return new VaultedSection
            {
                Header = "Vaulted Portfolio by Category Split",
                Total = new Money(items.Sum(x => x.AmountMoney), statement.CurrencyCode).ToString(),
                Items = items
            };
        }

        private decimal CalculateEffectiveValue(Document statement, StatementVaultItem vaultedItem)
        {
            if (string.IsNullOrEmpty(vaultedItem?.MetalType)) return decimal.Zero;
            return vaultedItem.WeightInVault * statement.MetalPriceDictionary.TryGet(vaultedItem.MetalType);
        }

        //Build Vaulted group by Sub Category
        private VaultedSection GetVaultedBySubCategory(Document statement)
        {
            var items = statement.StatementVaultItems
                .Where(x => !string.IsNullOrEmpty(x.MetalType))
                .GroupBy(x => new { x.MetalType, x.BullionType })
                .OrderBy(x => _fixedMetalOrder.IndexOf(x.Key.MetalType))
                .ThenBy(x => _fixedCategoryOrder.IndexOf(x.Key.BullionType.ToString()))
                .Select(x => new VaultedItem
                {
                    Name = x.Key.BullionType.Equals(Enums.BullionVariantType.Signature) ?
                        $"{x.Key.BullionType} {x.Key.MetalType}" :
                        $"{x.Key.MetalType} {x.Key.BullionType}s",
                    Quantity = x.Sum(y => y.WeightInVault).ToString("0.###"),
                    AmountMoney = new Money(x.Sum(y => CalculateEffectiveValue(statement, y)), statement.CurrencyCode)
                }).ToArray();

            return new VaultedSection
            {
                Header = "Vaulted Portfolio by Sub-Category Split",
                Total = new Money(items.Sum(x => x.AmountMoney), statement.CurrencyCode).ToString(),
                Items = items
            };
        }

        //Build Vaulted group by Range
        private VaultedSection GetVaultedByRange(Document statement)
        {
            var items = statement.StatementVaultItems
                .Where(x => x.Range != null)
                .OrderBy(x => x.Range.Rank)
                .GroupBy(x => new { x.Range.DisplayName })
                .Select(x => new VaultedItem
                {
                    Name = $"{x.Key.DisplayName}",
                    Quantity = x.Sum(y => y.WeightInVault).ToString("0.###"),
                    AmountMoney = new Money(x.Sum(y => CalculateEffectiveValue(statement, y)), statement.CurrencyCode)
                }).ToArray();

            return new VaultedSection
            {
                Header = "Vaulted Portfolio by Range Split",
                Total = new Money(items.Sum(x => x.AmountMoney), statement.CurrencyCode).ToString(),
                Items = items
            };
        }

        //Build Vaulted group by Product
        private VaultedSection GetVaultedByProduct(Document statement)
        {
            var items = statement.StatementVaultItems
                .Where(x => x.Variant != null)
                .GroupBy(x => x.VariantCode)
                .Select(x => new VaultedItem
                {
                    Name = $"{x.FirstOrDefault()?.Variant.DisplayName}",
                    Quantity = x.Sum(y => y.QuantityInVault).ToString("####"),
                    Weight = x.Sum(y => y.WeightInVault).ToString("0.###"),
                    AmountMoney = new Money(x.Sum(y => CalculateEffectiveValue(statement, y)), statement.CurrencyCode)
                }).OrderBy(x => x.Name).ToArray();

            return new VaultedSection
            {
                Header = "Vaulted Portfolio by Product",
                Total = new Money(items.Sum(x => x.AmountMoney), statement.CurrencyCode).ToString(),
                Items = items
            };
        }

        //Build PeriodActivity
        private PeriodActivity GetPeriodActivity(Document statement)
        {
            var items = statement.StatementTransactions
                .OrderBy(x => x.Date)
                .Select(x => new PeriodActivityItem
                {
                    Date = $"{x.Date:M/d/yyyy}",
                    Type = x.Type.ToEnum(Enums.StatementActivityType.Miscellaneous).GetDescriptionAttribute(),
                    Description = x.Variant != null ? x.Variant.DisplayName : x.Description,
                    Reference = x.ReferenceNumber,
                    Status = x.Status,
                    AmountMoney = new Money(x.Amount, statement.CurrencyCode)
                }).ToArray();

            var openBalance = statement.StatementHeader.OpeningBalance;

            foreach (var periodActivityItem in items)
            {
                periodActivityItem.BalanceMoney = new Money(openBalance + periodActivityItem.AmountMoney.Amount,
                    statement.CurrencyCode);

                openBalance = periodActivityItem.BalanceMoney.Amount;
            }

            return new PeriodActivity
            {
                Header = "Period Activity",
                StartDate = $"{statement.FromDate:M/d/yyyy}",
                EndDate = $"{statement.ToDate:M/d/yyyy}",
                StartBalance = new Money(statement.StatementHeader.OpeningBalance, statement.CurrencyCode).ToString(),
                Items = items,
                EndBalance = new Money(statement.StatementHeader.ClosingBalance, statement.CurrencyCode).ToString()
            };
        }
    }
}