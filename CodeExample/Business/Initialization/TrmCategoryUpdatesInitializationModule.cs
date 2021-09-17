using System;
using System.Threading.Tasks;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using TRM.Web.Models.Catalog;
using TRM.Web.Services.ProductBadge;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class TrmCategoryUpdatesInitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            contentEvents.PublishedContent += ContentEventsOnPublishedContent;
        }

        private void ContentEventsOnPublishedContent(object sender, ContentEventArgs e)
        {
            var category = e.Content as TrmCategoryBase;
            if (category == null)
            {
                return;
            }

            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(x =>
            {
                CachedProductBadgeRepository.InvalidateCache();
            });
        }

        public void Uninitialize(InitializationEngine context)
        {
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            contentEvents.PublishedContent -= ContentEventsOnPublishedContent;
        }
    }
}