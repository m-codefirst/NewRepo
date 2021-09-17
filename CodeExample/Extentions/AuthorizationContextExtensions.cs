using System.Web.Mvc;

namespace TRM.Web.Extentions
{
    public static class AuthorizationContextExtensions
    {
        public static void Redirect(this AuthorizationContext filterContext, bool jsRedirect, string url)
        {
            if (jsRedirect)
            {
                filterContext.Result = new JavaScriptResult()
                {
                    Script = $"<script type=\"text/javascript\">window.location='{url}';</script>"
                };
            }
            else
            {
                filterContext.Result = new RedirectResult($"~{url}");
            }
        }
    }
}