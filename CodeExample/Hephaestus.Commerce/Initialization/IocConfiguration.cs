using System.Diagnostics;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.CustomerServices;
using Hephaestus.Commerce.CustomerServices.AddressService;
using Hephaestus.Commerce.CustomerServices.ContactSearchService;
using Hephaestus.Commerce.CustomerServices.CustomerContactService;
using Hephaestus.Commerce.CustomerServices.CustomerOrdersService;
using Hephaestus.Commerce.CustomerServices.Orders;
using Hephaestus.Commerce.Helpers;
using Hephaestus.Commerce.Product.ProductService;
using InitializationModule = EPiServer.Web.InitializationModule;

namespace Hephaestus.Commerce.Initialization
{
    [ModuleDependency(typeof(InitializationModule))]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    [InitializableModule]
    public class IocConfiguration : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void Preload(string[] parameters)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            Debug.Assert(context != null, "context != null");
            context.StructureMap().Configure(
                ce =>
                {
                    //ce.For<IAmCatalogContentLoader>().Use<HephaestusCatalogContentLoader>();
                    ce.For<IAmReferenceConverter>().Use<HephaestusReferenceConverter>();
                    ce.For<ICustomerContactService>().Use<CustomerContactService>();
                    ce.For<ICustomerAddressService>().Use<CustomerAddressService>();
                    ce.For<IAmMetaDataHelper>().Use<MetaDataHelper>();
                    ce.For<ICustomerOrdersService>().Use<CustomerOrderService>();
                    ce.For<IAmContactSearchService>().Use<ContactSearchService>();
                    ce.For<IAmOrderContext>().Use<DefaultOrderContext>();
                });
        }
    }
}

