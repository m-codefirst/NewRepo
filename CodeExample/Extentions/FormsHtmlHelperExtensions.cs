//using EPiBootstrapArea;
using EPiServer.Core;
using EPiServer.Data.Entity;
using EPiServer.Forms.Core;
using EPiServer.Forms.Core.Models;
using EPiServer.Forms.Implementation.Elements;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Mvc.Html;
using Hephaestus.CMS.Interfaces;
using Hephaestus.ContentTypes.Business.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TRM.Web.Extentions
{
    public static class FormsHtmlHelperExtensions
    {
        private static Injected<IContentAreaLoader> _contentAreaLoader;

        public static void RenderFormElementsExt(this HtmlHelper html, int currentStepIndex, IEnumerable<IFormElement> elements, FormContainerBlock model)
        {
            foreach (var element in elements)
            {
                var areaItem = model.ElementsArea.Items.FirstOrDefault(i => i.ContentLink == element.SourceContent.ContentLink);

                if (areaItem != null)
                {
                    var cssClasses = GetItemCssClass(html, areaItem);
                    html.ViewContext.Writer.Write($"<div class=\"{cssClasses}\">");
                }

                var sourceContent = element.SourceContent;
                if (sourceContent != null && !sourceContent.IsDeleted)
                {
                    if (sourceContent is ISubmissionAwareElement)
                    {
                        var contentData = (sourceContent as IReadOnly).CreateWritableClone() as IContent;
                        (contentData as ISubmissionAwareElement).FormSubmissionId = (string)html.ViewBag.FormSubmissionId;
                        html.RenderContentData(contentData, false);
                    }
                    else
                    {
                        html.RenderContentData(sourceContent, false);
                    }
                }

                if (areaItem != null)
                    html.ViewContext.Writer.Write("</div>");
            }
        }

        private static string GetItemCssClass(HtmlHelper htmlHelper, ContentAreaItem contentAreaItem)
        {
            var displayOption = _contentAreaLoader.Service.LoadDisplayOption(contentAreaItem);
            var tag = displayOption != null ? displayOption.Tag : htmlHelper.ViewContext.ViewData["tag"] as string;
            return GetDisplayOptionClasses(tag);
        }

        private static string GetDisplayOptionClasses(string tag)
        {
            var contentAreaTags = ServiceLocator.Current.GetInstance<IContentAreaTags>();

            if (tag == contentAreaTags.HalfWidth)
            {
                return string.Format("{0} {1}",
                    StringConstants.GridClasses.ColXs12,
                    StringConstants.GridClasses.ColSm6);
            }
            if (tag == contentAreaTags.OneThirdWidth)
            {
                return string.Format("{0} {1}",
                    StringConstants.GridClasses.ColXs12,
                    StringConstants.GridClasses.ColSm4);
            }
            if (tag == contentAreaTags.OneQuarterWidth)
            {
                return string.Format("{0} {1}",
                    StringConstants.GridClasses.ColXs12,
                    StringConstants.GridClasses.ColSm3);
            }
            if (tag == contentAreaTags.OneSixthWidth)
            {
                return string.Format("{0} {1}",
                    StringConstants.GridClasses.ColXs12,
                    StringConstants.GridClasses.ColSm2);

            }
            if (tag == contentAreaTags.TwoThirdsWidth)
            {
                return string.Format("{0} {1}",
                    StringConstants.GridClasses.ColXs12,
                    StringConstants.GridClasses.ColSm8);
            }
            if (tag == contentAreaTags.ThreeQuartersWidth)
            {
                return string.Format("{0} {1}",
                    StringConstants.GridClasses.ColXs12,
                    StringConstants.GridClasses.ColSm9);
            }
            return string.Format("{0} {1}",
                StringConstants.GridClasses.ColXs12,
                StringConstants.GridClasses.ColSm12);
        }
    }
}