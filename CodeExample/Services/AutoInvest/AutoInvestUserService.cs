using System;
using System.Collections.Generic;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Models.DTOs.LittleTreasures;

namespace TRM.Web.Services.AutoInvest
{
    public class AutoInvestUserService : IAutoInvestUserService
    {
        private readonly CustomerContext _customerContext;
        private readonly IAutoInvestmentSerializationHelper _autoInvestmentSerializationHelper;

        public AutoInvestUserService(CustomerContext customerContext, IAutoInvestmentSerializationHelper autoInvestmentSerializationHelper)
        {
            _customerContext = customerContext;
            _autoInvestmentSerializationHelper = autoInvestmentSerializationHelper;
        }

        public Dictionary<string, decimal> GetInvestmentOptions(CustomerContact contact)
        {
            var investments = contact.GetStringProperty(StringConstants.CustomFields.AutoInvestProductCode);
            var amount = contact.GetDecimalProperty(StringConstants.CustomFields.AutoInvestAmount);

            return _autoInvestmentSerializationHelper.DeserializeInvestments(investments, amount);
        }

        public CustomerContact SaveInvestmentOptions(CustomerContact contact, Dictionary<string, decimal> investments,
            int autoInvestDay)
        {
            var totalAmount = _autoInvestmentSerializationHelper.GetTotalAmount(investments);
            var serializedInvestments = _autoInvestmentSerializationHelper.SerializeInvestments(investments);

            contact.UpdateProperty(StringConstants.CustomFields.IsAutoInvest, true);
            contact.UpdateProperty(StringConstants.CustomFields.AutoInvestAmount, totalAmount);
            contact.UpdateProperty(StringConstants.CustomFields.AutoInvestProductCode, serializedInvestments);
            contact.UpdateProperty(StringConstants.CustomFields.AutoInvestDay, autoInvestDay);
            contact.UpdateProperty(StringConstants.CustomFields.AutoInvestApplicationDate, DateTime.Now);

            contact.SaveChanges();
            return _customerContext.GetContactByUserId(contact.UserId);
        }

        public int GetInvestmentDay(CustomerContact contact)
        {
            return contact.GetIntegerProperty(StringConstants.CustomFields.AutoInvestDay);
        }

        public bool IsAutoInvestActive(CustomerContact contact)
        {
            return contact.GetBooleanProperty(StringConstants.CustomFields.IsAutoInvest);
        }

        public CustomerContact StopAutoInvest(CustomerContact contact)
        {
            contact.UpdateProperty(StringConstants.CustomFields.IsAutoInvest, false);
            contact.UpdateProperty(StringConstants.CustomFields.AutoInvestAmount, (decimal)0);
            contact.UpdateProperty(StringConstants.CustomFields.AutoInvestProductCode, string.Empty);
            contact.UpdateProperty(StringConstants.CustomFields.AutoInvestDay, 0);

            contact.SaveChanges();
            return _customerContext.GetContactByUserId(contact.UserId);
        }

        public void UpdateLastAutoInvestStatus(string contactUserId, Constants.Enums.AutoInvestUpdateOrderStatus status)
        {
            var contact = _customerContext.GetContactByUserId(contactUserId);
            this.UpdateLastAutoInvestStatus(contact, status);
        }
        public void UpdateLastAutoInvestStatus(CustomerContact contact, Constants.Enums.AutoInvestUpdateOrderStatus status)
        {
            contact.UpdateProperty(StringConstants.CustomFields.LastAutoInvestStatus, (int)status);
            contact.UpdateProperty(StringConstants.CustomFields.LastAutoInvestDate, DateTime.Now);
            contact.SaveChanges();
        }

        private static CustomerAddress CreateCustomerAddress(ContactDto contact)
        {
            var address = CustomerAddress.CreateInstance();
            address.AddressType = CustomerAddressTypeEnum.Public;
            address.IsDefault = true;
            if (!string.IsNullOrWhiteSpace(contact.Address.PostCode))
            {
                address.PostalCode = contact.Address.PostCode;
            }
            address.Line1 = contact.Address.Line1;
            address.Line2 = contact.Address.Line2;
            address.City = contact.Address.City;
            if (!string.IsNullOrWhiteSpace(contact.Address.CountryRegionId))
            {
                address.CountryCode = contact.Address.CountryRegionId;
            }
            address[StringConstants.AddressFieldNames.County] = contact.Address.County;
            return address;
        }
    }
}