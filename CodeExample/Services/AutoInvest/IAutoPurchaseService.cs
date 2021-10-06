using System.Collections.Generic;
using TRM.Web.Models.EntityFramework.CustomerContactContext;

namespace TRM.Web.Services.AutoInvest
{
    public interface IAutoPurchaseService
    {
        List<AutoPurchaseProcessedUserDto> UpdateContactsInRange(IEnumerable<cls_Contact> contacts);
    }
}