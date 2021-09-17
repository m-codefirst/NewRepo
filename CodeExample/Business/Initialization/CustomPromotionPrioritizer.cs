using System.Linq;
using EPiServer;
using EPiServer.Commerce.Marketing;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using TRM.Web.Business.Promotions;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class CustomPromotionPrioritizer : IInitializableModule
    {
        public const int Step = 5;

        private void SetDefaultPromotionPriority(object sender, ContentEventArgs e)
        {
            if (!(e.Content is PromotionData content) || content is ZeroPricePromotion || content.Priority != 0)
            {
                return;
            }

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

            var allPromotion = contentLoader.GetChildren<SalesCampaign>(SalesCampaignFolder.CampaignRoot)
                .SelectMany(c => contentLoader.GetChildren<PromotionData>(c.ContentLink))
                .ToList();

            var lowestNonZeroPromotionPriority = allPromotion
                .Where(x => !(x is ZeroPricePromotion))
                .Select(x => x.Priority)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            content.Priority = lowestNonZeroPromotionPriority + Step;
        }

        public void Initialize(InitializationEngine context)
        {
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            contentEvents.CreatingContent += this.SetDefaultPromotionPriority;
        }

        public void Uninitialize(InitializationEngine context)
        {
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            contentEvents.CreatingContent -= this.SetDefaultPromotionPriority;
        }
    }
}