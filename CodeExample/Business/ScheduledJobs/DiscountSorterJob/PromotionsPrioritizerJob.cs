using System.Linq;
using EPiServer;
using EPiServer.Commerce.Marketing;
using EPiServer.DataAccess;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using TRM.Web.Business.Promotions;

namespace TRM.Web.Business.ScheduledJobs.DiscountSorterJob
{
    [ScheduledPlugIn(DisplayName = "[Commerce] Promotion prioritizer", Description = "Push ZeroPricePromotions to the end of the list of priorities.")]
    public class PromotionsPrioritizerJob : ScheduledJobBase
    {
        public const int Addition = 1000;

        private readonly IContentRepository repository;
        
        public PromotionsPrioritizerJob(IContentRepository repository)
        {
            this.repository = repository;
        }

        public override string Execute()
        {
            var allPromotion = this.repository.GetChildren<SalesCampaign>(SalesCampaignFolder.CampaignRoot)
                .SelectMany(c => this.repository.GetChildren<PromotionData>(c.ContentLink))
                .ToList();

            var zeroPricePromotions = allPromotion.Where(x => x is ZeroPricePromotion)
                .Cast<ZeroPricePromotion>()
                .ToList();

            var lowestNonZeroPromotionPriority = allPromotion
                .Where(x => !(x is ZeroPricePromotion))
                .Select(x => x.Priority)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            for (int i = 0; i < zeroPricePromotions.Count; i++)
            {
                var writable = (ZeroPricePromotion) zeroPricePromotions[i].CreateWritableClone();
                writable.Priority = lowestNonZeroPromotionPriority + Addition + i;
                repository.Save(writable, SaveAction.Publish);
            }

            return $"Found {allPromotion.Count} promotions and {zeroPricePromotions.Count} zero price promos";
        }
       
    }
}