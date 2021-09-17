using EPiServer.Shell;
using TRM.Web.Models.Pages.Bullion;

namespace TRM.Web.Business.UIDescriptors
{
    [UIDescriptorRegistration]
    public class DeliverBullionDefaultLandingPageUiDescriptor : UIDescriptor<DeliverBullionLandingPage>
    {
        public DeliverBullionDefaultLandingPageUiDescriptor() : base(ContentTypeCssClassNames.Page)
        {
            DefaultView = CmsViewNames.AllPropertiesView;
            DisabledViews = new[] { CmsViewNames.OnPageEditView };
        }
    }
}