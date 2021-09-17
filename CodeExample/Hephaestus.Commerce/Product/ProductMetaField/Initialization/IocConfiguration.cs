using System.Diagnostics;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.Product.ProductMetaField.Service;
using InitializationModule = EPiServer.Web.InitializationModule;

namespace Hephaestus.Commerce.Product.ProductMetaField.Initialization
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
            context.StructureMap().Configure(ce => ce.For<IProductMetaFieldService>().Use<ProductMetaFieldService>());
        }
    }
}
