using EPiServer.Core;
using System.Collections.Generic;
using TRM.Web.Models.Articles;

namespace TRM.Web.Services
{
    public interface ICommentService
    {
        bool PostComment(ContentReference articleId, UserComment userComment);
        IEnumerable<UserComment> GetComments(ContentReference articleId);
    }
}
