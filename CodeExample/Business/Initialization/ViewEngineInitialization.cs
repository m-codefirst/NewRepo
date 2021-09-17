using System.Web.Mvc;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Web;
using TRM.Web.Business.Rendering;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class ViewEngineInitialization : IInitializableModule
    {
        private bool _initialized;
        public void Initialize(InitializationEngine context)
        {
            if (_initialized) return;

            ViewEngines.Engines.Add(new TrmViewEngine());

            context.Locate.TemplateResolver().TemplateResolved += TemplateCoordinator.OnTemplateResolved;

            _initialized = true;
        }

        public void Uninitialize(InitializationEngine context)
        {
            if (!_initialized) return;
            context.Locate.TemplateResolver().TemplateResolved -= TemplateCoordinator.OnTemplateResolved;
            _initialized = false;
        }

        public void Preload(string[] parameters)
        {
        }
    }
}