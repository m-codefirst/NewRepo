using TRM.Web.Models.DTOs;

namespace TRM.Web.Services
{
    public interface ITrmInventoryService
    {
        OutSourceCodesDto GetOutSourcedAndSourceCodes(string catalogEntryCode);
    }
}