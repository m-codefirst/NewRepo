using EPiServer.ServiceLocation;
using TRM.Web.Helpers;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Extentions
{
    public static class ArticleExtensions
    {
        public static ArticleCommentsViewModel LoadComments(this ArticlePage page, bool isAnonymous)
        {
            var articleHelper = ServiceLocator.Current.GetInstance<IHaveCommentHelper>();
            return articleHelper.GetArticleComments(page.ContentLink, isAnonymous);
        }
    }

}