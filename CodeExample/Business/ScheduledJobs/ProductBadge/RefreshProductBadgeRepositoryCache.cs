using EPiServer.PlugIn;
using EPiServer.Scheduler;
using TRM.Web.Services.ProductBadge;

namespace TRM.Web.Business.ScheduledJobs.ProductBadge
{

    [ScheduledPlugIn(DisplayName = "[ProductBadge] Refresh ProductBadgeRepository cache")]
    public class RefreshProductBadgeRepositoryCache : ScheduledJobBase
    {
        public override string Execute()
        {
            CachedProductBadgeRepository.InvalidateCache();

            return "Cache refreshed. Reload the page to see the result.";
        }
    }
}