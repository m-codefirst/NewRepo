using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Find;
using EPiServer.Find.ClientConventions;
using EPiServer.Find.Commerce;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace TRM.Web.Business.Initialization
{
    public class TrmCatalogContentClientConventions : CatalogContentClientConventions
    {
        private static readonly ILogger __logger = LogManager.GetLogger();

        protected override void ApplyNestedConventions(NestedConventions nestedConventions)
        {
            try
            {
                nestedConventions.ForInstancesOf<IPricing>().Add(pricing => pricing.Prices());
            }


            catch (ServiceException)
            {
                __logger.Error("A ServiceException..");
            }
            try
            {
                nestedConventions.ForInstancesOf<IIndexedPrices>().Add(pricing => pricing.Prices());
            }


            catch (ServiceException)
            {
                __logger.Error("A ServiceException..");
            }
            try
            {
                nestedConventions.ForInstancesOf<IStockPlacement>().Add(stockPlacement => stockPlacement.Inventories());
            }


            catch (ServiceException)
            {
                __logger.Error("A ServiceException..");
            }
        }
    }

    [ModuleDependency(typeof(FindCommerceInitializationModule))]
    public class EpiserverFindInitializationModule : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<CatalogContentClientConventions, TrmCatalogContentClientConventions>();
        }

        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }

}