using EPiServer.DataAbstraction;
using EPiServer.Framework.Web;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Mvc;
using EPiServer.Web.Routing.Segments.Internal;
using Hephaestus.CMS.Interfaces;
using Hephaestus.CMS.Models.Blocks;
using Mediachase.Commerce;
using TRM.Web.Helpers.Find;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Interfaces.EntryProperties;

namespace TRM.Web.Business.Rendering
{
    [ServiceConfiguration(typeof(IViewTemplateModelRegistrator), Lifecycle = ServiceInstanceScope.Singleton)]
    public class TemplateCoordinator : IViewTemplateModelRegistrator
    {
   
        public const string BlockFolder = "~/Views/Default/Blocks/";
        public const string BlockFolderForMultiSite = "~/Views/" + BespokePathPlaceHolder + "/Blocks/";
        public const string PrototypeBlocksFolderForMultiSite = "~/Views/" + BespokePathPlaceHolder + "/Blocks/Prototypes/";
        public const string PrototypeSharedFolderForMultiSite = "~/Views/Shared/Prototypes/";
        public const string MultiSiteBlockPartialsFolder = "~/Views/" + BespokePathPlaceHolder + "/BlockPartials/";
        public const string ImageFileFolderForMultiSite = "~/Views/" + BespokePathPlaceHolder + "/ImageFile/";
        public const string ImageFileFolder = "~/Views/Default/ImageFile/";

        public const string PageFolder = "~/Views/Default/Pages/";
        public const string PagePartialsFolder = "~/Views/Default/PagePartials/";
        public const string PageFolderForMultiSite = "~/Views/" + BespokePathPlaceHolder + "/Pages/";
        public const string PrototypeFolderForMultiSite = "~/Views/" + BespokePathPlaceHolder + "/Pages/Prototypes/";

        public const string MultiSitePagePartialsFolder = "~/Views/" + BespokePathPlaceHolder + "/PagePartials/";
        public const string MultiSitePaymentMethodsFolder = "~/Views/" + BespokePathPlaceHolder + "/PagePartials/PaymentMethods/";
        public const string MultiSiteGdprPagePartialFolder = "~/Views/" + BespokePathPlaceHolder + "/PagePartials/GDPR/";

        public const string BespokePathPlaceHolder = "%%^";


        public static void OnTemplateResolved(object sender, TemplateResolverEventArgs args)
        {
            var iControlPublishedState = args.ItemToRender as IControlPublishedState;
            
            if (iControlPublishedState != null && !iControlPublishedState.PublishOntoSite)
            {
                if (RequestSegmentContext.CurrentContextMode != ContextMode.Preview && RequestSegmentContext.CurrentContextMode != ContextMode.Edit)
                args.SelectedTemplate = null;
            }

            var iAmCommerceSearchable = args.ItemToRender as IAmCommerceSearchable;

            if (iAmCommerceSearchable != null && (!iAmCommerceSearchable.PublishOntoSite || iAmCommerceSearchable.Restricted && args.RequestedCategory != TemplateTypeCategories.Page))
            {
                if (RequestSegmentContext.CurrentContextMode != ContextMode.Preview && RequestSegmentContext.CurrentContextMode != ContextMode.Edit)
                    args.SelectedTemplate = null;
            }

            var iControlTrmMarketFiltering = args.ItemToRender as IControlTrmMarketFiltering;
            if (iControlTrmMarketFiltering == null) return;
            var currentMarket = ServiceLocator.Current.GetInstance<ICurrentMarket>();
            if ((iControlTrmMarketFiltering.UnavailableMarkets?.Contains(currentMarket.GetCurrentMarket().MarketId.Value) ?? false) && args.RequestedCategory != TemplateTypeCategories.Page)
            {
                args.SelectedTemplate = null;
            }
        }

        public void Register(TemplateModelCollection viewTemplateModelRegistrator)
        {
            var contentAreaTags = ServiceLocator.Current.GetInstance<IContentAreaTags>();
            viewTemplateModelRegistrator.Add(typeof(HephaestusBlock), new TemplateModel
            {
                Name = "NoRendererMessage",
                Inherit = true,
                Tags = new[] { contentAreaTags.NoRenderer },
                AvailableWithoutTag = false,
                Path = BlockPath("NoRenderer")
            });
        }

        private static string BlockPath(string name)
        {
            return string.Format("{0}{1}.cshtml", BlockFolder, name);
        }
    }
}