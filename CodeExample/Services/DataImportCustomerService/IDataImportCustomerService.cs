using System.Collections.Generic;
using Mediachase.Commerce.Customers;
using static TRM.Web.Models.DTOs.Bullion.AxImportData;

namespace TRM.Web.Services.DataImportCustomerService
{
    public interface IDataImportCustomerService
    {
        bool CreateCustomerFromAx(CustomerRequest customer);
        bool UpdateCustomerFromAx(CustomerRequest axCustomer, CustomerContact customerToUpdate);
        bool UpdateCustomerAddressesFromAx(List<Address> axAddresses,
            CustomerContact contact, bool isBullionUser);

        bool UpdateContactFromAx(ObsCustomerContactRequest contactRequest, CustomerContact customerToUpdate);
    }
}