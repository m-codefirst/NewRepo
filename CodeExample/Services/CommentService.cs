using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Web.Models.Articles;
using TRM.Web.Models.Pages;

namespace TRM.Web.Services
{
    public class CommentService : ICommentService
    {
        private readonly IContentRepository _contentRepository;
        private readonly IContentLoader _contentLoader;
        private readonly ISynchronizedObjectInstanceCache _synchronizedObjectInstanceCache;
        public CommentService(IContentRepository contentRepository,
            IContentLoader contentLoader,
            ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache)
        {
            _contentRepository = contentRepository;
            _contentLoader = contentLoader;
            _synchronizedObjectInstanceCache = synchronizedObjectInstanceCache;
        }
        public IEnumerable<UserComment> GetComments(ContentReference articleId)
        {
            var result = new List<UserComment>();
            var key = $"article_{articleId}::comments";
            var cached = _synchronizedObjectInstanceCache.Get<List<UserComment>>(key, ReadStrategy.Immediate);
            if (cached != null) return cached;
            var article = _contentLoader.Get<IContent>(articleId) as ArticlePage;
            if (null == article) return result;
            result = article.UserComments.ToList();
            _synchronizedObjectInstanceCache.Insert(key, result, new CacheEvictionPolicy(TimeSpan.FromMinutes(5), CacheTimeoutType.Absolute));
            return result;
        }

        public bool PostComment(ContentReference articleId, UserComment userComment)
        {
            var article = _contentRepository.Get<ContentData>(articleId).CreateWritableClone() as ArticlePage;
            if (null == article) return false;
            var comments = article.UserComments ?? new List<UserComment>();
            comments.Add(userComment);
            article.UserComments = comments;
            _contentRepository.Save(article, EPiServer.DataAccess.SaveAction.Publish, AccessLevel.NoAccess);
            return true;
        }
    }
}
