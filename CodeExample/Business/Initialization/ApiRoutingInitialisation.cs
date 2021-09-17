using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Framework.FrameworkInitialization))]
    public class ApiRoutingInitialisation : IInitializableModule
    {
        public const string DefaultApiRouteName = "DefaultApiRoute";
        private bool _initialized;

        public void Initialize(InitializationEngine context)
        {
            if (_initialized) return;
            RouteTable.Routes.MapMvcAttributeRoutes();
            GlobalConfiguration.Configuration.MapHttpAttributeRoutes();

            _initialized = true;
        }

        public void Uninitialize(InitializationEngine context)
        {
            _initialized = false;
        }

        public void Preload(string[] parameters)
        { }
    }
}