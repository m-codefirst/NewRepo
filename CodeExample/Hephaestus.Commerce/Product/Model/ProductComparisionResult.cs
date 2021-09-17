using System.Collections.Generic;

namespace Hephaestus.Commerce.Product.Model
{
    public class ProductComparisionProperties
    {
        public string PropertyHeading { get; set; }
        public List<string> Values { get; set; }

        public ProductComparisionProperties(string propertyHeading)
        {
            PropertyHeading = propertyHeading;
            Values = new List<string>();
        }
    }
}
