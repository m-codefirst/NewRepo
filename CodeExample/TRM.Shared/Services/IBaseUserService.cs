using Mediachase.Commerce.Customers;
using TRM.Shared.Models.DTOs;

namespace TRM.Shared.Services
{
    public interface IBaseUserService
    {
        ContactIdentityResult RegisterAccount(ApplicationUser user, bool isBullionAccount = false);
        ApplicationUser GetUser(string email);
        bool IsEmailAvailable(string email);
        CustomerContact GetExistingContactByUserName(string userName);
    }
}
