using System;
using System.Collections.Generic;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.CustomerServices.AddressService
{
    public interface ICustomerAddressService
    {
        IEnumerable<CustomerAddress> AllAddresses { get; }

        CustomerAddress GetSpecificAddress(PrimaryKeyId? primaryKeyId);

        IEnumerable<CustomerAddress> GetSpecificAddresses(Func<CustomerAddress, bool> expr);

        PrimaryKeyId? DefaultShippingAddressId { get; set; }
        
        PrimaryKeyId? DefaultBillingAddressId { get; set; }

        void CreateAddress(CustomerAddress customerAddress);

        void Update(CustomerAddress customerAddress);

        void Delete(PrimaryKeyId? primaryKeyId);
    }
}
