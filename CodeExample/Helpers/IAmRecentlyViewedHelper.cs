using System.Collections.Generic;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Helpers
{
    public interface IAmRecentlyViewedHelper
    {
        List<RecentlyViewedDto> GetRecentlyViewedListProducts(string currentvariant);
        void SetRecentlyViewedCookie(string sku);
    }
}