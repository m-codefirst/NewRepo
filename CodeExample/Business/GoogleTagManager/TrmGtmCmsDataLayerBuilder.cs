using System.Web.Mvc;
using EPiServer.Core;
using Hephaestus.CMS.Business.Gtm;
using Newtonsoft.Json.Linq;
using TRM.Web.Helpers;

namespace TRM.Web.Business.GoogleTagManager
{

    public class TrmGtmCmsDataLayerBuilder : GtmDataLayerBuilder
    {
        private readonly IAmGoogleTagManagerHelper _googleTagManagerHelper;
        private readonly IAmContactHelper _contactHelper;

        public TrmGtmCmsDataLayerBuilder(IAmGoogleTagManagerHelper googleTagManagerHelper, IAmContactHelper contactHelper)
        {
            _googleTagManagerHelper = googleTagManagerHelper;
            _contactHelper = contactHelper;
        }

        protected override void PushContentDataInternal(JObject dataLayer, ContentData contentData, bool isPartial,
            ActionExecutingContext filterContext)
        {
            
        }

        protected override void PushContentDataInternal(JObject dataLayer, ContentData contentData, ActionExecutingContext filterContext)
        {
            var customer = _contactHelper.GetCheckoutCustomerContact();
            if (customer != null &&
                (customer.Properties.Contains(Shared.Constants.StringConstants.CustomFields.ObsAccountNumber) ||
                 customer.Properties.Contains(Shared.Constants.StringConstants.CustomFields.BullionObsAccountNumber)))
            {
                dataLayer["ObsAccountNumber"] = customer
                    .Properties[Shared.Constants.StringConstants.CustomFields.ObsAccountNumber]?.Value?.ToString();
                dataLayer["BullionObsAccountNumber"] = customer
                    .Properties[Shared.Constants.StringConstants.CustomFields.BullionObsAccountNumber]?.Value
                    ?.ToString();
            }

            _googleTagManagerHelper.CheckForCommercePages(dataLayer, contentData, filterContext);
        }

        protected override void PushBlockDataInternal(JObject dataLayer, BlockData blockData, ActionExecutingContext filterContext)
        {
            
        }
    }
}