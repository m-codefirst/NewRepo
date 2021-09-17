using System.Collections.Generic;
using System.Linq;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Shared.Models;

namespace TRM.Web.Helpers
{
    public class AddressHelper : IAmAddressHelper
    {
        private readonly IAddressBookService _addressBookService;


        public AddressHelper(IAddressBookService addressBookService)
        {
            _addressBookService = addressBookService;
        }

        public IEnumerable<AddressModel> GetAddressModelList()
        {
            return _addressBookService.List().Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.CountryCode));
        }

        public IEnumerable<AddressModel> GetAddressModelListForConsumerCheckout()
        {
            return GetAddressModelList().Where(x => !x.Name.Equals(Shared.Constants.StringConstants.PreciousMetalsRegisteredAddressName));
        }
    }
}