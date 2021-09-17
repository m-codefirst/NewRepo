using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Castle.Core.Internal;
using EPiServer.Find.Helpers.Text;
using EPiServer.Logging;
using TRM.Web.Extentions;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.EntityFramework.FeefoReviews;

namespace TRM.Web.Business.Feefo
{
    public class FeefoReviewService : IAmAReviewService
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(FeefoReviewService));

        public virtual FeefoReviewDto ReviewData(string sku)
        {
            return GetReviewDataFromFeefoApi(sku);
        }

        public virtual FeefoReviewDto ReviewData(string sku, int skip, int take)
        {
            var model = GetReviewDataFromFeefoApi(sku);
            var newList = model.ReviewData.OrderByDescending(r => r.ReviewDate).Skip(skip).Take(take).ToArray();
            model.ReviewData = newList;
            return model;
        }

        private FeefoReviewDto GetReviewDataFromFeefoApi(string sku)
        {
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null)
            {
                _logger.Error(
                    "An issue occurred whilst getting or processing the Feefo Data Feed. Returned an Empty review list.");
                return GetEmptyReviewDto();
            }

            if (string.IsNullOrWhiteSpace(sku))
            {
                throw new ArgumentNullException(sku);
            }

            try
            {
                {
                    if (startPage.FeefoReviewFeed.IsNullOrWhiteSpace())
                    {
                        return GetEmptyReviewDto();
                    }

                    var url = startPage.FeefoReviewFeed + sku;

                    var feefoxml = new XmlDocument();
                    feefoxml.Load(url);
                    if (feefoxml.IsNullOrEmpty())
                    {
                        _logger.Error(
                            "The Feefo Review Feed has returned zero reviews from Feefo, please ensure that this has been set properly on the Feefo Review Feed property on the Start Page.");
                        return GetEmptyReviewDto();
                    }

                    var xnlistfeedback = feefoxml.SelectNodes("/FEEDBACKLIST/FEEDBACK");
                    var xnliststatistics = feefoxml.SelectSingleNode("/FEEDBACKLIST/SUMMARY");
                    if (xnlistfeedback == null || xnliststatistics == null)
                    {
                        return GetEmptyReviewDto(); 
                    }

                    var reviewDto = GetEmptyReviewDto();

                    using (var db = new FeefoReviewContext(Shared.Constants.StringConstants.TrmCustomDatabaseName))
                    {
                        var reviewsToRemove = db.FeefoReviewDataModels.Where(frdm => frdm.Sku == sku);
                        if (reviewsToRemove.Any())
                        {
                            db.FeefoReviewDataModels.RemoveRange(reviewsToRemove.ToArray());
                            db.SaveChanges();
                        }

                        var reviews = new List<ReviewDataModel>();
                        foreach (XmlNode xmlfeedbacknode in xnlistfeedback)
                        {
                            var reviewModel = new ReviewDataModel {Id = Guid.NewGuid(), Sku = sku};
                            var xmlelementdate = xmlfeedbacknode["DATE"];
                            reviewModel.PresentationReviewDate = GetStringValueFromNode(xmlelementdate);
                            var xmlelementhrreviewdate = xmlfeedbacknode["HREVIEWDATE"];
                            reviewModel.ReviewDate = GetDateValueFromNode(xmlelementhrreviewdate);
                            var xmlelementhrviewrating = xmlfeedbacknode["HREVIEWRATING"];
                            reviewModel.ReviewRating = GetIntegerValueFromNode(xmlelementhrviewrating);
                            var xmlelementproductrating = xmlfeedbacknode["PRODUCTRATING"];
                            reviewModel.ProductRating = GetStringValueFromNode(xmlelementproductrating);
                            var xmlelementservicerating = xmlfeedbacknode["SERVICERATING"];
                            reviewModel.ServiceRating = GetStringValueFromNode(xmlelementservicerating);
                            var xmlElementVendorComment = xmlfeedbacknode["VENDORCOMMENT"];
                            reviewModel.MerchantComment = GetStringValueFromNode(xmlElementVendorComment);
                            var xmlElementAuthorName = xmlfeedbacknode["PUBLICCUSTOMER"]?["NAME"];
                            reviewModel.ReviewAuthor = GetStringValueFromNode(xmlElementAuthorName);

                            var customerComment = GetStringValueFromNode(xmlfeedbacknode["CUSTOMERCOMMENT"]);

                            if (string.IsNullOrWhiteSpace(customerComment))
                            {
                                reviewModel.ProductReview = string.Empty;
                                reviewModel.ServiceReview = string.Empty;
                            }
                            else
                            {
                                var match = Regex.Match(customerComment, @"^Service rating : (.*)Product : (.*)?$",
                                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase |
                                    RegexOptions.Multiline);
                                if (match.Success && match.Groups.Count > 2)
                                {
                                    reviewModel.ProductReview = match.Groups[2].Value;
                                    reviewModel.ServiceReview = match.Groups[1].Value;
                                }
                                else
                                {
                                    reviewModel.ServiceReview = customerComment;
                                    reviewModel.ProductReview = customerComment;
                                }
                            }

                            reviews.Add(reviewModel);

                            db.FeefoReviewDataModels.Add(reviewModel);
                        }

                        db.SaveChanges();

                        reviewDto.CountOfFeedbacks = reviews.Count;
                        reviewDto.ReviewData = reviews.ToArray();
                    }
                    
                    reviewDto.CountOfReviews = GetIntegerValueFromNode(xnliststatistics["COUNT"]);
                    reviewDto.StarRating = GetDecimalValueFromNode(xnliststatistics["FIVESTARAVERAGE"]);

                    return reviewDto;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "An issue occurred whilst getting or processing the Feefo Data Feed. Returned an Empty review list.",
                    ex);
                return new FeefoReviewDto();
            }
        }

        private string GetStringValueFromNode(XmlNode node)
        {
            return node?.InnerText ?? string.Empty;
        }

        private DateTime GetDateValueFromNode(XmlNode node)
        {
            if (node == null)
            {
                _logger.Warning("GetDateValueFromNode: The Feefo node is null.");
                return DateTime.MinValue;
            }

            DateTime reviewDate;
            if (DateTime.TryParse(node.InnerText, out reviewDate))
            {
                return reviewDate;
            }

            _logger.Warning("GetDateValueFromNode: Unable to parse Date Value from [{0}]. The value was [{1}].",
                node.Name, node.InnerText);
            return DateTime.MinValue;
        }

        private int GetIntegerValueFromNode(XmlNode node)
        {
            if (node == null)
            {
                _logger.Warning("GetIntegerValueFromNode: The Feefo XmlNode is null");
                return 0;
            }

            int i;
            if (int.TryParse(node.InnerText, out i))
            {
                return i;
            }

            _logger.Warning(
                "GetIntegerValueFromNode: The Feefo XmlNode {0} won't parse to an Integer; value was [{1}].",
                node.Name, node.InnerText);
            return 0;
        }

        private decimal GetDecimalValueFromNode(XmlNode node)
        {
            if (node == null)
            {
                _logger.Warning("GetDecimalValueFromNode: The Feefo XmlNode is null");
                return decimal.Zero;
            }

            decimal i;
            if (decimal.TryParse(node.InnerText, out i))
            {
                return i;
            }

            _logger.Warning("GetDecimalValueFromNode: The Feefo XmlNode {0} won't parse to a decimal; value was [{1}].",
                node.Name, node.InnerText);
            return decimal.Zero;
        }

        private static FeefoReviewDto GetEmptyReviewDto()
        {
            return new FeefoReviewDto
            {
                ReviewData = new List<ReviewDataModel>(),
                StarRating = 0,
                CountOfReviews = 0
            };
        }
    }
}