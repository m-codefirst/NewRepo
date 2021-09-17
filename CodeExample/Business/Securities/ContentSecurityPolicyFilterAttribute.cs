using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace TRM.Web.Business.Securities
{
    public class ContentSecurityPolicyFilterAttribute : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!filterContext.RequestContext.HttpContext.Items.Contains(nameof(ContentSecurityPolicyFilterAttribute)))
            {
                var cspConfig = ConfigurationManager.AppSettings["ContentSecurityPolicy"];
                if (!string.IsNullOrWhiteSpace(cspConfig))
                {
                    HttpResponseBase response = filterContext.HttpContext.Response;
                    response.AddHeader("Content-Security-Policy", cspConfig);
                    filterContext.RequestContext.HttpContext.Items.Add(nameof(ContentSecurityPolicyFilterAttribute), string.Empty);
                }
            }
        }
    }
}