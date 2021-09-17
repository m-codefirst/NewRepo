using System.Web.Mvc;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Hephaestus.CMS.Business.Gtm;
using TRM.Web.Business.GoogleTagManager;
using TRM.Web.Business.Securities;
using TRM.Web.Filters;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class FilterConfig : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            GlobalFilters.Filters.Add(new RequireHttpsAttribute());
            GlobalFilters.Filters.Add(new PromCodeAttrbute());
            GlobalFilters.Filters.Add(new LockedUserAttribute());
            GlobalFilters.Filters.Add(ServiceLocator.Current.GetInstance<AddCmsToGtmDataLayerActionFilter>());
            GlobalFilters.Filters.Add(ServiceLocator.Current.GetInstance<AddCommerceToGtmDataLayerActionFilter>());
            GlobalFilters.Filters.Add(ServiceLocator.Current.GetInstance<ContentSecurityPolicyFilterAttribute>());
        }

        public void Uninitialize(InitializationEngine context)
        {

        }

        public void Preload(string[] parameters)
        { }
    }
}