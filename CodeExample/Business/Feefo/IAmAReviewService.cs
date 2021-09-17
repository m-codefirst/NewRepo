using TRM.Web.Models.DTOs;

namespace TRM.Web.Business.Feefo
{
    public interface IAmAReviewService
    {
        FeefoReviewDto ReviewData(string sku);
        FeefoReviewDto ReviewData(string sku, int skip, int take);
    }
}
