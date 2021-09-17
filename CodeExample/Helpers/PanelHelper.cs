using System.Web.Mvc;
using EPiServer.Web.Mvc.Html;
using Hephaestus.ContentTypes.Business.Extensions;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.ViewModels;
using TRM.Shared.Extensions;

namespace TRM.Web.Helpers
{
    public class PanelHelper : IPanelHelper
    {
        private readonly UrlHelper _urlHelper;

        public PanelHelper(UrlHelper urlHelper)
        {
            _urlHelper = urlHelper;
        }

        public PanelViewModel GetPanelViewModel(PanelBlock panel)
        {

            var hoverImg = string.Empty;

            if (panel.CustomImage != null) hoverImg = _urlHelper.ContentUrlExtension(panel.CustomImage.Image);
            
            var model = new PanelViewModel
            {
                ThisBlock = panel,
                HoverAlignment = panel.HoverContentAlignment.DescriptionAttr(),
                HoverTextAlignment = panel.HoverTextAlignment.DescriptionAttr(),
                HoverFgColour = panel.HoverContentColour.DescriptionAttr(),
                HoverBgColour = panel.HoverContentBackgroundColour.DescriptionAttr(),
                DefaultBgColour = panel.BackgroundColour.DescriptionAttr(),
                DefaultFgColour = panel.ForeColour.DescriptionAttr(),
                DefaultAlignment = panel.ContentAlignment.DescriptionAttr(),
                DefaultTextAlignment = panel.TextAlignment.DescriptionAttr(),
                Padding = panel.Padding.DescriptionAttr(),
                DefaultWidth = panel.ContentWidth.DescriptionAttr(),
                HoverWidth = panel.HoverContentWidth.DescriptionAttr(),
                HoverImage = hoverImg,
                ContentBorder = panel.ContentBorder.DescriptionAttr(),
                HoverContentBorder = panel.HoverContentBorder.DescriptionAttr()
            };

            model.HasDefaultContent = (model.ThisBlock.Content != null && !model.ThisBlock.Content.IsEmpty) || !string.IsNullOrWhiteSpace(model.ThisBlock.Heading);
            model.HasHoverContent = (model.ThisBlock.HoverContent != null && !model.ThisBlock.HoverContent.IsEmpty) ||
                                !string.IsNullOrWhiteSpace(model.ThisBlock.HoverContentHeading) || (model.ThisBlock.LinkHyperlink != null && !model.ThisBlock.LinkHyperlink.IsEmpty() )||
                                !string.IsNullOrWhiteSpace(model.HoverImage);

            return model;
        }
        
    }
}
