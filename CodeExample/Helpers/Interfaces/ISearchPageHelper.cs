using EPiServer.Core;
using EPiServer.Find.Cms;
using System.Collections.Generic;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers.Interfaces
{
    public interface ISearchPageHelper
    {
        IEnumerable<ArticleWithBreadcrumbViewModel> GetArticleWithBreadcrumbViewModel(IContentResult<IContentData> content, ITrmLayoutModel layoutModel, bool processBreadcrumb = true);
    }
}
