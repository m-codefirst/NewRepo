using System;
using System.Linq;
using StackExchange.Profiling;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.EntityFramework.FeefoReviews;

namespace TRM.Web.Business.Feefo
{
    public class CachedFeefoReviewService :FeefoReviewService
    {
        private readonly MiniProfiler _miniProfiler;

        public CachedFeefoReviewService()
        {
            _miniProfiler = MiniProfiler.Current;

            using (_miniProfiler.Step("Loading Cached Review "))
            {
            }
        }

        public override FeefoReviewDto ReviewData(string sku)
        {
          
            return GetOrCreateCachedEntry(sku);
        }

        public override FeefoReviewDto ReviewData(string sku, int skip, int take)
        {
            try
            {
                var review = GetOrCreateCachedEntry(sku);
                review.ReviewData = review.ReviewData?.Skip(skip).Take(take).ToArray();
                return review;
            } catch (Exception)
            {
                return new FeefoReviewDto();
            }
            
        }
        private FeefoReviewDto GetOrCreateCachedEntry(string sku)
        {
            
            using (_miniProfiler.Step("Database activity "))
            {

                using (var db = new FeefoReviewContext(Shared.Constants.StringConstants.TrmCustomDatabaseName))
                {
                    var dbReviews = db.FeefoReviews.Where(m => m.Sku == sku);

                    var minDateForLastSavedReviews = DateTime.UtcNow - TimeSpan.FromMinutes(10);//TODO: Hard coded 10 minutes

                    if (dbReviews.Any(x => x.AsOf < minDateForLastSavedReviews))
                    {
                        var reviewsToRemove = dbReviews
                            .Where(x => x.AsOf < minDateForLastSavedReviews && x.Sku == sku);
                        db.FeefoReviews.RemoveRange(reviewsToRemove.ToArray());
                        db.SaveChanges();
                    }

                    var dbReview = db.FeefoReviews.FirstOrDefault(m => m.Sku == sku);
                    if (dbReview != null)
                    {
                        var reviewData = db.FeefoReviewDataModels
                            .Where(frdm => frdm.Sku == dbReview.Sku)
                            .OrderByDescending(r => r.ReviewDate);

                        try
                        {
                            return new FeefoReviewDto
                            {
                                ReviewData = reviewData.ToList(),
                                CountOfReviews = dbReview.CountOfReviews,
                                CountOfFeedbacks = reviewData.Count(),
                                StarRating = dbReview.StarRating,
                            };
                        }
                        catch (Exception ex)
                        {
                            return null;
                        }
                    }

                    var newReviewViewModel = base.ReviewData(sku, 0, 250); //TODO: Hard coded 250 records

                    db.FeefoReviews.Add(new Review
                    {
                        AsOf = DateTime.UtcNow,
                        CountOfReviews = newReviewViewModel.CountOfReviews,
                        ReviewData = newReviewViewModel.ReviewData.ToArray(),
                        Sku = sku,
                        StarRating = newReviewViewModel.StarRating,
                        Id = Guid.NewGuid()
                    });

                    db.SaveChanges();
                    return newReviewViewModel;
                }
            }
        }
    }
}