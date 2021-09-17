using System.ComponentModel;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.PlugIn;
using EPiServer.Shell.ObjectEditing;
using TRM.Web.Business.SelectionFactories;
using TRM.Web.CustomProperties;

namespace TRM.Web.Business.DataAccess.GlobalPurchaseLimit
{
    public class GlobalPurchaseLimitSetting
    {
        [SelectOne(SelectionFactoryType = typeof(TrmEnumSelectionFactory<PricingAndTradingService.Models.Constants.MetalType>))]
        [BackingType(typeof(PropertyNumber))]
        [DisplayName("Metal Type")]
        public PricingAndTradingService.Models.Constants.MetalType MetalType { get; set; }

        [DisplayName("Position")]
        public decimal Position { get; set; }

        [DisplayName("Volume")]
        public decimal Volume { get; set; }

        [DisplayName("Signature Volume")]
        public decimal SignatureVolume { get; set; }
    }

    [PropertyDefinitionTypePlugIn]
    public class GlobalPurchaseLimitSettingListProperty : PropertyListBase<GlobalPurchaseLimitSetting> { }
}