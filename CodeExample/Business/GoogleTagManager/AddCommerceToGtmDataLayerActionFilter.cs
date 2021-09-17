using System;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Framework.Web;
using EPiServer.Web;
using EPiServer.Web.Routing.Segments.Internal;
using Hephaestus.CMS.Business.Gtm;

namespace TRM.Web.Business.GoogleTagManager
{
    public class AddCommerceToGtmDataLayerActionFilter : IActionFilter
    {
        protected readonly IBuildCommerceGtmDataLayer GtmDataLayerBuilder;

        public AddCommerceToGtmDataLayerActionFilter(IBuildCommerceGtmDataLayer gtmDataLayerBuilder)
        {
            GtmDataLayerBuilder = gtmDataLayerBuilder;
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");
            if ((filterContext.IsChildAction || filterContext.ActionDescriptor.IsDefined(typeof(SkipGtmDataLayerAttribute), true) ? 1 : (filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(SkipGtmDataLayerAttribute), true) ? 1 : 0)) != 0)
                return;
            switch (RequestSegmentContext.CurrentContextMode)
            {
                case ContextMode.Edit:
                    break;
                case ContextMode.Preview:
                    break;
                default:
                    HandleRequest(filterContext);
                    break;
            }
        }

        protected virtual ContentData ValidateAndConvertContent(object actionParameter)
        {
            return actionParameter as CatalogContentBase;
        }

        protected virtual void HandleRequest(ActionExecutingContext filterContext)
        {
            if (!filterContext.ActionParameters.Any())
                return;
            var contentData = this.ValidateAndConvertContent(filterContext.ActionParameters.First().Value);
            if (contentData == null)
            {
                return;
            }

            var descriptorAttribute = filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(TemplateDescriptorAttribute), true).FirstOrDefault() as TemplateDescriptorAttribute;

            if (descriptorAttribute == null ||
                descriptorAttribute.TemplateTypeCategory != TemplateTypeCategories.MvcController &&
                descriptorAttribute.TemplateTypeCategory != TemplateTypeCategories.Mvc)
            {
                return;
            }

            GtmDataLayerBuilder.Push(contentData, filterContext);
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }
    }
}
