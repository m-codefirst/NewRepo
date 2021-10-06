using System.Collections.Generic;
using TrmWallet.Entity;
using TRM.Web.Models.EntityFramework.BullionPortfolio;
using TRM.Web.Models.ViewModels.Bullion;

namespace TRM.Web.Services.DeliverFromVault
{
    public interface IBullionDeliverFromVaultService
    {
        DeliverBullionLandingViewModel BuildDeliverBullionLandingViewModel(string variantCode, decimal? quantityToDeliver = null);
        DeliverBullionLandingViewModel BuildDeliverBullionLandingViewModel(ITrmDeliverTransaction deliverTransaction);
        TrmDeliverTransaction SaveDeliverTransaction(SellOrDeliverBullionViewModel deliverBullionViewModel, ref Dictionary<string, string> messages);
    }
}