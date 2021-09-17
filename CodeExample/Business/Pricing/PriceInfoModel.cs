namespace TRM.Web.Business.DataAccess
{
    public class PriceInfoModel
    {
        public PriceInfoModel()
        {
        }
        public PriceInfoModel( decimal price, string variantCode, EpiPriceBullionKeyInfoModel priceKeyInfo)
        {
            Price = price;
            VariantId = variantCode;
            PriceKeyInfoModel = priceKeyInfo;
        }

        public decimal Price { get; set; }
        public string VariantId { get; set; }

        public EpiPriceBullionKeyInfoModel PriceKeyInfoModel { get; set; }
    }

}
