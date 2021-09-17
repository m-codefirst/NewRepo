using System.Collections.Generic;

namespace Hephaestus.Commerce.Shared.Models
{
    public class CountryRegionModel
    {
        public IEnumerable<string> RegionOptions { get; set; }

        public string Region { get; set; }
    }
}
