using EPiServer.Commerce.Order;
using EPiServer.Core;
using Mediachase.Commerce;
using Newtonsoft.Json.Linq;

namespace TRM.Web.Helpers
{
    public interface IAmGoogleTagManagerHelper
    {
        JObject GetEntryObject(ContentReference contentReference, decimal quantity = decimal.Zero);
        JObject GetLineItemObject(ILineItem lineItem, Currency currency);
        JObject GetCartObjectForGtm();
        JObject GetNodeObject(ContentReference contentReference);
        void CheckForCommercePages(JObject dataLayer, ContentData contentData, System.Web.Mvc.ActionExecutingContext filterContext);
    }
}