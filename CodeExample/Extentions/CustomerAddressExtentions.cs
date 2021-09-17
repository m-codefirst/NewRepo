using Mediachase.Commerce.Customers;
using static TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Extentions
{
    public static class CustomerAddressExtentions
    {
        public static bool IsBullionKYCAddress(this CustomerAddress address)
        {
            if (address?.Properties[CustomFields.BullionKycAddress] == null)
                return false;

            return address.Properties[CustomFields.BullionKycAddress].Value != null && address.Properties[CustomFields.BullionKycAddress].Value.Equals(true);
        }
    }
}