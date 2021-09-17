using System.Collections.Generic;

namespace Hephaestus.Commerce.Shared.Models
{
    public class AddressModel
    {
        private string _postCode;

        public AddressModel()
        {
            CountryRegion = new CountryRegionModel();
        }

        public string AddressId { get; set; }

        public string Name { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string CountryName { get; set; }

        public string CountryCode { get; set; }

        public IEnumerable<CountryModel> CountryOptions { get; set; }

        public string City { get; set; }

        public string PostalCode { get => _postCode; set => _postCode = value?.ToUpper(); }

        public string Line1 { get; set; }

        public string Line2 { get; set; }

        //[UIHint("AddressRegion")]
        public CountryRegionModel CountryRegion { get; set; }

        public string Email { get; set; }

        public bool ShippingDefault { get; set; }

        public bool BillingDefault { get; set; }

        public string DaytimePhoneNumber { get; set; }

        public string Organization { get; set; }

        public string ErrorMessage { get; set; }
    }
}
