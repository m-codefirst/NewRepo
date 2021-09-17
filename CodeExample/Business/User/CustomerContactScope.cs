using EPiServer.Find.Helpers;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;

namespace TRM.Web.Business.User
{
    public class CustomerContactScope : PrincipalScope
    {
        public CustomerContactScope(CustomerContact contact) : base(GetUsername(contact), ServiceLocator.Current.GetInstance<IUserImpersonation>())
        {
        }
        
        private static string GetUsername(CustomerContact contact)
        {
            if (contact.IsNull()) return string.Empty;
            var mapUserKey = new MapUserKey();
            return mapUserKey.ToUserKey(contact.UserId)?.ToString() ?? contact.UserId;
        }
    }
}