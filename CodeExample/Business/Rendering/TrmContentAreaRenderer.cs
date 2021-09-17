using System.IO;
using System.Text;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using EPiServer.Web.Mvc;
using Hephaestus.ContentTypes.Business.Rendering;
using Hephaestus.ContentTypes.Business.Extensions;
using TRM.Web.Constants;
using TRM.Web.Helpers;
using TRM.Web.Models.Interfaces;

namespace TRM.Web.Business.Rendering
{
    public class TrmContentAreaRenderer : BaseContentAreaRenderer
    {
        private readonly IContentLoader _contentLoader;
        private readonly IGiftingHelper _giftingHelper;
        public const string ChildrenCustomTagNameKey = "childrencustomtagname";
        public const string TagKey = "tag";
        public const string HasContainerKey = "hascontainer";
        public const string CustomTagKey = "customtag";
        public const string HasEditContainerKey = "HasEditContainer";
        public string ContentAreaTag { get; private set; }

        public TrmContentAreaRenderer(IContentRenderer contentRenderer, TemplateResolver templateResolver,
            IContentAreaItemAttributeAssembler attributeAssembler, IContentRepository contentRepository, IContentLoader contentLoader, IContentAreaLoader areaLoader, IGiftingHelper giftingHelper)
            : base(contentRenderer, templateResolver, attributeAssembler, contentRepository, contentLoader, areaLoader)
        {
            _contentLoader = contentLoader;
            _giftingHelper = giftingHelper;
        }

        public override void Render(HtmlHelper htmlHelper, ContentArea contentArea)
        {
            if (contentArea == null || contentArea.IsEmpty)
                return;
            
            // capture given CA tag (should be contentArea.Tag, but EPiServer is not filling that property)
            ContentAreaTag = htmlHelper.ViewData["tag"] as string;

            var viewContext = htmlHelper.ViewContext;
            var tagBuilder = (TagBuilder)null;
            if (!IsInEditMode(htmlHelper) && ShouldRenderWrappingElement(htmlHelper))
            {
                tagBuilder = new TagBuilder(GetContentAreaHtmlTag(htmlHelper, contentArea));
                AddNonEmptyCssClass(tagBuilder, viewContext.ViewData["cssclass"] as string);
                viewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));
            }
            RenderContentAreaItems(htmlHelper, contentArea.FilteredItems);

            if (tagBuilder == null)
                return;
            viewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.EndTag));
        }

        /// <summary>
        /// Updated from the base to default to False
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <returns></returns>
        protected new bool ShouldRenderWrappingElement(HtmlHelper htmlHelper)
        {
            var nullable = (bool?)htmlHelper.ViewContext.ViewData["hascontainer"];
            return nullable.HasValue && nullable.Value;
        }


        protected override void RenderContentAreaItem(
          HtmlHelper htmlHelper,
          ContentAreaItem contentAreaItem,
          string templateTag,
          string htmlTag,
          string cssClass)
        {
            var originalWriter = htmlHelper.ViewContext.Writer;
            var tempWriter = new StringWriter();
            htmlHelper.ViewContext.Writer = tempWriter;

            try
            {
                // persist selected DisplayOption for content template usage (if needed there of course)

                using (new ContentAreaItemContext(htmlHelper.ViewContext.ViewData, contentAreaItem))
                {
                    // NOTE: if content area was rendered with tag (Html.PropertyFor(m => m.Area, new { tag = "..." }))
                    // this tag is overridden if editor chooses display option for the block
                    // therefore - we need to persist original CA tag and ask kindly EPiServer to render block template in original CA tag context
                    var tag = string.IsNullOrEmpty(ContentAreaTag) ? templateTag : ContentAreaTag;

                    if (!_giftingHelper.CanShowContent(contentAreaItem.ContentLink))
                    {
                        return;
                    }
                    
                    base.RenderContentAreaItem(htmlHelper, contentAreaItem, tag, htmlTag, cssClass);

                    var contentItemContent = tempWriter.ToString();
                    var hasEditContainer = htmlHelper.ViewContext.ViewData[HasEditContainerKey] as string;

                    // we need to render block if we are in Edit mode
                    if (IsInEditMode(htmlHelper) && !string.IsNullOrEmpty(hasEditContainer))
                    {
                        originalWriter.Write(contentItemContent);
                        return;
                    }

                    originalWriter.Write(contentItemContent);
                }
            }
            finally
            {
                // restore original writer to proceed further with rendering pipeline
                htmlHelper.ViewContext.Writer = originalWriter;
            }
        }

        protected override string GetContentAreaItemCssClass(HtmlHelper htmlHelper, ContentAreaItem contentAreaItem)
        {
            var css = new StringBuilder(base.GetContentAreaItemCssClass(htmlHelper, contentAreaItem));

            var content = _contentLoader.Get<IContent>(contentAreaItem.ContentLink);

            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsHoverEffect = content as IControlHoverEffect;
            if (controlsHoverEffect != null)
            {
                css.AppendWithSpace(controlsHoverEffect.HoverEffect.DescriptionAttr());
            }
            
            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsBackgroundAlignment = content as IControlBackgroundImageAlignment;
            if (controlsBackgroundAlignment != null)
            {
                css.AppendWithSpace(controlsBackgroundAlignment.BackgroundImageAlignment.DescriptionAttr());
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsTextAlignment = content as IControlTextAlignment;
            if (controlsTextAlignment != null)
            {
                css.AppendWithSpace(controlsTextAlignment.TextAlignment.DescriptionAttr());
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsPullUp = content as IControlPullUp;
            if (controlsPullUp != null)
            {
               if (controlsPullUp.PullUp) css.AppendWithSpace(StringConstants.CSS.PullUp);
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsBannerHeight = content as IControlBannerHeight;
            if (controlsBannerHeight != null)
            {
                css.AppendWithSpace(controlsBannerHeight.BannerHeightClass);
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsStretch = content as IControlStretcher;
            if (controlsStretch != null)
            {
                if (controlsStretch.Stretch)
                {
                    css.Replace(StringConstants.Blocks.ContainerBlock, StringConstants.Blocks.StretcherBlock);
                }
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsIconPosition = content as IControlIconPosition;
            if (controlsIconPosition != null)
            {
                css.AppendWithSpace(controlsIconPosition.IconPosition.DescriptionAttr());
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsContentWidth = content as IControlContentWidth;
            if (controlsContentWidth != null)
            {
                css.AppendWithSpace(controlsContentWidth.ContentWidth.DescriptionAttr());
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var controlsBorderColour = content as IControlBorderColour;
            if (controlsBorderColour != null)
            {
                css.AppendWithSpace(controlsBorderColour.BorderColourClass);
            }

            return css.ToString().Trim();
        }
      
    }
}