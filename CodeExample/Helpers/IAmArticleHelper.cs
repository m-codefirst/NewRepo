using System.Collections.Generic;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public interface IAmArticleHelper
    {
        ArticleListingViewModel GetArticleListingViewModel(ArticleListingBlock articleListingBlock);

        List<TeaserViewModel> GetArticles(LoadMoreDto loadMore);
    }
}