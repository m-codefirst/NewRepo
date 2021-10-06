using System;
using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Customers;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.ViewModels;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Models.ViewModels.Bullion.Portfolio;

namespace TRM.Web.Services.Portfolio
{
    public interface IBullionPortfolioService
    {
        PortfolioViewModel CreatePortfolioViewModel();
        VaultContentViewModel CreateVaultContentViewModel();
        VaultedInvestmentBlockViewModel CreateVaultedInvestmentBlockViewModel();
		MetalChartBlockViewModel CreateMetalChartBlockViewModel();
        bool CreatePortfolioContentsWhenPurchase(IPurchaseOrder purchaseOrder);
        bool CreatePortfolioContentsWhenPurchase(IPurchaseOrder purchaseOrder, CustomerContact customerContact);
        bool HasOutstandingBuyOrSellOrDeliverTransaction(Guid customerId);
        bool ImportPortfolioFromAx(AxImportData.BullionHoldingsCustomer customerWithStoredItems,
            Guid customerId);
        IEnumerable<PortfolioVariantItemModel> GetPortfolioVariantItems();
        PortfolioMetalModel GetPortfolioMetalModel(IEnumerable<PortfolioVariantItemModel> portfolioVariants,
            PricingAndTradingService.Models.Constants.MetalType metalType);
    }
}