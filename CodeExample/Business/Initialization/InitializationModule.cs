using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Web;
using EPiServer.Web.Routing;
using System.Linq;
using System.Web.Routing;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class InitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {

            var referenceConverter = context.Locate.Advanced.GetInstance<Mediachase.Commerce.Catalog.ReferenceConverter>();
            var contentLoader = context.Locate.Advanced.GetInstance<IContentLoader>();

            var catalogs = contentLoader.GetChildren<CatalogContentBase>(referenceConverter.GetRootLink()).ToList();
            if (!catalogs.Any()) return;

            var siteDefinitionRepository = context.Locate.Advanced.GetInstance<ISiteDefinitionRepository>();
            var siteDefinitions = siteDefinitionRepository.List();
            foreach (var siteDefinition in siteDefinitions)
            {
                var catalogPartialRouter = new HierarchicalCatalogPartialRouter(() => siteDefinition.StartPage, catalogs.First(), false);
                RouteTable.Routes.RegisterPartialRouter(catalogPartialRouter);
            }
        }

        public void Preload(string[] parameters) { }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}