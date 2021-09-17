using EPiServer.Core;
using TRM.Web.Models.Articles;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public interface IHaveCommentHelper
    {
        ArticleCommentsViewModel GetArticleComments(ContentReference articleId, bool isAnonymous);
        bool PostComment(ContentReference articleId, UserComment userComment);
        void SendCommentEmailApproval(string pageUrl, UserComment userComment);
    }
}