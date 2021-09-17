using EPiServer.Shell;
using TRM.Web.Models.Pages.Bullion;

namespace TRM.Web.Business.UIDescriptors
{
    [UIDescriptorRegistration]
    public class SellBullionDefaultLandingPageUiDescriptor : UIDescriptor<SellBullionDefaultLandingPage>
    {
        public SellBullionDefaultLandingPageUiDescriptor() : base(ContentTypeCssClassNames.Page)
        {
            DefaultView = CmsViewNames.AllPropertiesView;
            DisabledViews = new[] { CmsViewNames.OnPageEditView };
        }
    }
}