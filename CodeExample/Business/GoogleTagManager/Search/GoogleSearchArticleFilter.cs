using EPiServer.Cms.Shell;
using EPiServer.Web.Routing;
using Hephaestus.CMS.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Web.Mvc;
using TRM.Web.Models.Pages;
using TRM.Shared.Extensions;
using EPiServer;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using TRM.Web.Models.Blocks;
using EPiServer.Web;
using TRM.Web.Extentions;
using EPiServer.Core;
using TRM.Shared.Constants;

namespace TRM.Web.Business.GoogleTagManager.Search
{
    public class GoogleSearchArticleDto
    {
        public string Id;
        public string Author;
        public string Publisher;
        public string Heading;
        public DateTime Published;
        public DateTime Modified;
        public string LogoUrl;
        public string[] ImageUrls;
    }

    public class GoogleSearchArticleBuilder
    {
        public static JObject AsJObject(GoogleSearchArticleDto dto)
        {
            return new JObject(
                new JProperty("@context", "https://schema.org"),
                new JProperty("@type", "Article"),
                new JProperty("mainEntityOfPage",
                    new JObject(
                        new JProperty("@type", "WebPage"),
                        new JProperty("@id", dto.Id)
                    )
                ),
                new JProperty("headline", dto.Heading),
                new JProperty("image", new JArray(dto.ImageUrls)),
                new JProperty("datePublished", dto.Published.ToString("yyyy-MM-ddTHH:mm:ss")),
                new JProperty("dateModified", dto.Modified.ToString("yyyy-MM-ddTHH:mm:ss")),
                new JProperty("author",
                    new JObject(
                        new JProperty("@type", "Organization"),
                        new JProperty("name", dto.Author)
                    )
                ),
                new JProperty("publisher",
                    new JObject(
                        new JProperty("@type", "Organization"),
                        new JProperty("name", dto.Publisher),
                        new JProperty("logo",
                            new JObject(
                                new JProperty("@type", "ImageObject"),
                                new JProperty("url", dto.LogoUrl)
                            )
                        )
                    )
                )
            );
        }
    }

    public class GoogleSearchArticleFilter : ActionFilterAttribute
    {
        private readonly IContentLoader _contentLoader;
        public GoogleSearchArticleFilter()
        {
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            var mode = filterContext.RequestContext.GetContextMode();
            switch (mode)
            {
                case ContextMode.Edit:
                    break;
                case ContextMode.Preview:
                    break;
                case ContextMode.Default:
                case ContextMode.Undefined:
                default:
                    if (!filterContext.ActionParameters.Any())
                        return;

                    var contentData = filterContext.ActionParameters.First().Value as ArticlePage;
                    if (contentData == null || !contentData.IsPublished())
                        return;

                    // takes first image block in following content area, Content > Top Content
                    var contents = contentData.ThreeContentAreas.TopContent?.Items.Select(x => x.ContentLink).ToList();
                    if (contents == null || contents.All(x => x.ID == 0))
                    {
                        return;
                    }

                    var items = _contentLoader.GetItems(contents, ContentLanguage.PreferredCulture);
                    if(items == null)
                        return;

                    var imageBlock = items.FirstOrDefault(x => x is TrmImageBlock) as TrmImageBlock;

                    var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
                    var logoBlock = _contentLoader.Get<IContentData>(startPage.SiteLogo) as TrmImageBlock;

                    var dto = new GoogleSearchArticleDto
                    {
                        Id = contentData.CanonicalUrl?.ToString() ?? contentData.ContentLink.GetExternalUrl_V2(),
                        Heading = contentData.PageName,
                        Author = "The Royal Mint",
                        Publisher = "The Royal Mint",
                        Published = contentData.StartPublish.Value,
                        Modified = contentData.Changed,
                        LogoUrl = logoBlock?.LgImage?.GetExternalUrl_V2(),
                        ImageUrls = new[] { imageBlock?.LgImage?.GetExternalUrl_V2() },
                    };

                    filterContext.HttpContext.Items[StringConstants.GoogleSearch.Article] = GoogleSearchArticleBuilder.AsJObject(dto);

                    break;
            }
        }
    }
}