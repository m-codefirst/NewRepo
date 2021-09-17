using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer;
using EPiServer.Cms.Shell;
using EPiServer.Core;
using EPiServer.Find.Helpers;
using EPiServer.Framework.Localization;
using TRM.Web.Business.Email;
using TRM.Web.Constants;
using TRM.Web.Models.Articles;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels;
using TRM.Web.Services;

namespace TRM.Web.Helpers
{
    public class ArticleHelper : IAmArticleHelper, IHaveCommentHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly ICommentService _commentService;
        private readonly IAmTeaserHelper _teaserHelper;
        private readonly IEmailHelper _emailHelper;
        private readonly LocalizationService _localizationService;


        public ArticleHelper(IContentLoader contentLoader,
            IAmTeaserHelper teaserHelper,
            ICommentService commentService,
            IEmailHelper emailHelper,
            LocalizationService localizationService)
        {
            _emailHelper = emailHelper;
            _localizationService = localizationService;
            _contentLoader = contentLoader;
            _teaserHelper = teaserHelper;
            _commentService = commentService;
        }

        private CommentViewModel ToCommentViewModel(UserComment userComment)
        {
            return new CommentViewModel
            {
                Message = userComment.Message,
                CommentAt = userComment.CommentAt.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                Contact = userComment.ContactName
            };
        }

        public ArticleCommentsViewModel GetArticleComments(ContentReference articleId, bool isAnonymous)
        {
            var result = new ArticleCommentsViewModel { };
            var article = _contentLoader.Get<IContent>(articleId) as IHaveComment;
            if (null == article) return null;
            result.ArticleId = articleId;
            result.ShowCommentBox = article.CommentsCanBeLeft && article.CommentCount < article.MaximumNumberOfComment;
            result.AnonymousMsg = isAnonymous ? _localizationService.GetString(StringResources.ArticleCommentAnonymousText, StringConstants.ArticleCommentAnonymousText) : string.Empty;
            result.Heading = _localizationService.GetString(StringResources.ArticleCommentHeading, StringConstants.ArticleCommentHeading);
            result.SubHeading = _localizationService.GetString(StringResources.ArticleCommentSubHeading, StringConstants.ArticleCommentSubHeading);
            result.SubmitButtonText = _localizationService.GetString(StringResources.ArticleCommentSubmitButton, StringConstants.ArticleCommentSubmitButton);
            result.HighlightText = _localizationService.GetString(StringResources.ArticleCommentHighlightText, StringConstants.ArticleCommentHighlightText);
            result.MaximumMessageSize = _localizationService.GetString(StringResources.ArticleCommentMaximumMessage, StringConstants.ArticleCommentMaximumMessage);
            result.MaxinumWarningMessage= _localizationService.GetString(StringResources.ArticleCommentMaximumWarningMessage, StringConstants.ArticleCommentMaximumWarningMessage);
            var comments = article.UserComments != null && article.DisplayModeratedComments ? article.UserComments.Where(x => x.Approved).ToList() : new List<UserComment>();
            result.Comments = comments.Select(ToCommentViewModel).ToList();
            result.EnableComment = article.CommentsCanBeLeft || comments.Any();
            return result;
        }

        public ArticleListingViewModel GetArticleListingViewModel(ArticleListingBlock articleListingBlock)
        {

            var vm = new ArticleListingViewModel() { ThisBlock = articleListingBlock };

            if (articleListingBlock.ArticlesReference.IsNull()) return vm;

            vm.TotalArticles = _contentLoader.GetChildren<ArticlePage>(articleListingBlock.ArticlesReference).Count();

            vm.Articles = GetArticles(new LoadMoreDto() { ContentReference = articleListingBlock.ArticlesReference, ResultsPerPage = articleListingBlock.NumberOfArticlesToShow, Page = 1 });

            return vm;

        }

        public List<TeaserViewModel> GetArticles(LoadMoreDto loadMore)
        {
            var articles = _contentLoader.GetChildren<ArticlePage>(loadMore.ContentReference).Skip(((loadMore.Page - 1) * loadMore.ResultsPerPage)).Take(loadMore.ResultsPerPage).ToList();
            var thisList = new List<TeaserViewModel>();
            foreach (var article in articles)
            {
                var teaser = _teaserHelper.GetTeaserDto(article);

                var teaserViewModel = new TeaserViewModel() { Teaser = teaser, PageUrl = article.ToExternalUrl() };
                thisList.Add(teaserViewModel);

            }

            return thisList;
        }

        public bool PostComment(ContentReference articleId, UserComment userComment)
        {
            var article = _contentLoader.Get<ArticlePage>(articleId);
            if (article == null || !article.CommentsCanBeLeft || article.MaximumNumberOfComment <= article.CommentCount) return false;
            return _commentService.PostComment(articleId, userComment);
        }

        public void SendCommentEmailApproval(string pageUrl, UserComment userComment)
        {
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            if (null == startPage) return;

            ContentReference trmMEmailPageReference;
            string commentApprovalToEmail;
            string commentApprovalToEmailDisplayName;
            if (startPage.BlogSettingsPage != null)
            {
                var blogSettings = _contentLoader.Get<BlogSettingsPage>(startPage.BlogSettingsPage);
                trmMEmailPageReference = blogSettings.CommentApprovalEmail;
                commentApprovalToEmail = blogSettings.CommentApprovalToEmail;
                commentApprovalToEmailDisplayName = blogSettings.CommentApprovalToEmailDisplayName;
            }
            else
            {
                trmMEmailPageReference = startPage.CommentApprovalEmail;
                commentApprovalToEmail = startPage.CommentApprovalToEmail;
                commentApprovalToEmailDisplayName = startPage.CommentApprovalToEmailDisplayName;
            }

            var commentApprovalEmailTemplate = _contentLoader.Get<TRMEmailPage>(trmMEmailPageReference);
            if (null == commentApprovalEmailTemplate) return;
            var fullUrl = $"{HttpContext.Current.Request.Url.Scheme}://{HttpContext.Current.Request.Url.Host}{pageUrl}";
            _emailHelper.SendCommentApprovalEmail(commentApprovalEmailTemplate, commentApprovalToEmail, commentApprovalToEmailDisplayName, fullUrl, userComment);
        }
    }
}