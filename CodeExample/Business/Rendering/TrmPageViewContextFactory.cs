using EPiServer;
using EPiServer.Core;
using Hephaestus.CMS.Business.Rendering;
using Hephaestus.CMS.ViewModels;
using TRM.Web.Models.Layouts;

namespace TRM.Web.Business.Rendering
{
    public class TrmPageViewContextFactory : PageViewContextFactory
    {
        public TrmPageViewContextFactory(IContentLoader contentLoader) : base(contentLoader)
        {
        }

        protected override ILayoutModel CreateLayoutModel(IContent currentContent, IContent startPage)
        {
            return new TrmLayoutModel(currentContent, startPage);
        }

        protected override ILayoutModel CreateLayoutModel(IContent currentContent)
        {
            return new TrmLayoutModel(currentContent);
        }
    }
}