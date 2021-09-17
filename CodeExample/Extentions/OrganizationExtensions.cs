using System.Linq;
using Mediachase.Commerce.Customers;

namespace TRM.Web.Extentions
{
    public static class OrganizationExtensions
    {
        public static CustomerAddress GetFirstAddress(this Organization organization)
        {
            var address = organization.Addresses;
            if (address != null && address.Any())
            {
                return address.FirstOrDefault();
            }
            return null;
        }
    }
}