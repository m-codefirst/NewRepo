using EPiServer.Commerce.Order;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using System.Diagnostics;
using TRM.IntegrationServices.DataAccess;
using TRM.IntegrationServices.Helpers;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Services;
using TRM.Shared.Services;
using InitializationModule = EPiServer.Web.InitializationModule;

namespace CommerceManager
{
    [ModuleDependency(typeof(ServiceContainerInitialization), typeof(InitializationModule))]
    [InitializableModule]
    public class IocConfiguration : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            Debug.Assert(context != null, "context != null");
            context.StructureMap().Configure(ce =>
            {
                ce.For<ILineItemCalculator>().Use<TrmLineItemCalculator>().Singleton();
                ce.For<IBullionCustomerExportService>().Use<CustomersExportTransactionBase>().Singleton();
                ce.For<IExportTransactionsRepository>().Use<ExportTransactionsRepository>().Singleton();
                ce.For<IImpersonationLogService>().Use<ImpersonationLogService>().Singleton();
            });
        }

        public void Preload(string[] parameters)
        {
        }
    }
}