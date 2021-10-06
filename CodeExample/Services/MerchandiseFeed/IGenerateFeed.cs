using TRM.Web.Services.MerchandiseFeed.Pages;

namespace TRM.Web.Services.MerchandiseFeed
{
    public interface IGenerateFeed
    {
        void Generate(FeedPage feedPage);
    }
}