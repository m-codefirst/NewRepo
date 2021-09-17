using Castle.Core.Internal;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Api.Facets;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Cms;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.Helpers;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Extensions;
using TRM.Shared.Interfaces;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.Catalog.DDS;
using TRM.Web.Models.DDS;
using TRM.Web.Models.Find;
using TRM.Web.Models.Interfaces.EntryProperties;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers.Find
{
    public class FindHelper : IFindHelper
    {
        private readonly IAmStoreHelper _storeHelper;
        private readonly IAmAssetHelper _assetHelper;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly LocalizationService _localizationService;
        private readonly IAmTeaserHelper _teaserHelper;
        private readonly IGiftingHelper _giftingHelper;
        private readonly IBullionPriceHelper _bullionPriceHelper;
        private readonly IAmLocalPriceDataHelper _localPriceDataHelper;
        private readonly IAmSpecificationHelper _specificationHelper;
        private readonly IAnalyticsDigitalData _analyticsDigitalData;
        private readonly IAmVariantHelper _amVariantHelper;

        public FindHelper(IAmStoreHelper storeHelper, 
            IAmAssetHelper assetHelper, 
            IAmInventoryHelper inventoryHelper, 
            LocalizationService localizationService, 
            IAmTeaserHelper teaserHelper, 
            IGiftingHelper giftingHelper, 
            IBullionPriceHelper bullionPriceHelper, 
            IAmLocalPriceDataHelper localPriceDataHelper, 
            IAmSpecificationHelper specificationHelper,
            IAnalyticsDigitalData analyticsDigitalData,
            IAmVariantHelper amVariantHelper)
        {
            _storeHelper = storeHelper;
            _assetHelper = assetHelper;
            _inventoryHelper = inventoryHelper;
            _localizationService = localizationService;
            _teaserHelper = teaserHelper;
            _giftingHelper = giftingHelper;
            _bullionPriceHelper = bullionPriceHelper;
            _localPriceDataHelper = localPriceDataHelper;
            _specificationHelper = specificationHelper;
            _analyticsDigitalData = analyticsDigitalData;
            _amVariantHelper = amVariantHelper;
        }

        public ITypeSearch<IAmCommerceSearchable> AddCommerceFacet(ITypeSearch<IAmCommerceSearchable> search,
            IAddCommerceSearchFacets searchFacet)
        {
            if (searchFacet.FacetType == FacetType.Term)
            {
                return new Search<TrmVariant, IQuery>(search, context =>
                {
                    // ReSharper disable once UseStringInterpolation
                    var facetRequest = new TermsFacetRequest(searchFacet.Name)
                    {
                        Field = string.Format("{0}$${1}", searchFacet.Term, "string"),
                        Size = 100
                    };
                    context.RequestBody.Facets.Add(facetRequest);
                });
            }

            if (searchFacet.FacetType != FacetType.Range) return search;

            var ranges = new List<NumericRange>
            {
                new NumericRange {To = searchFacet.StartPoint - 1 }
            };
            int rangeStart;

            for (rangeStart = searchFacet.StartPoint; rangeStart + searchFacet.Increment < searchFacet.EndPoint; rangeStart = rangeStart + searchFacet.Increment)
            {
                //If we've added more than 20 then the UI can't nicely handle this
                //Also if an editor creates a range block with a tiny increment and an end point in the millions it'll bring the site down
                //This will ensure that the start and end are covered but we're preventing the site crashing from trying to render hundreds/thousands of options
                if (ranges.Count > 20)
                {
                    break;
                }

                ranges.Add(new NumericRange { From = rangeStart, To = rangeStart + searchFacet.Increment });
            }

            if (rangeStart < searchFacet.EndPoint)
            {
                ranges.Add(new NumericRange { From = rangeStart, To = searchFacet.EndPoint });
            }


            ranges.Add(new NumericRange { From = searchFacet.EndPoint });

            Action<NumericRangeFacetRequest> action = x => x.Ranges.AddRange(ranges);

            var searchTerm = searchFacet.Term == "ProductPrices" ? _localPriceDataHelper.GetPriceReference() + "$$number" : string.Format("{0}$${1}", searchFacet.Term, "number");

            return new Search<TrmVariant, IQuery>(search, context =>
            {
                var rangeFacetRequest = new NumericRangeFacetRequest(searchFacet.Name)
                {
                    Field = searchTerm
                };
                action(rangeFacetRequest);
                context.RequestBody.Facets.Add(rangeFacetRequest);
            });

        }

        public FindFacet ProcessFacet(IContentResult<IAmCommerceSearchable> baseResult, IContentResult<IAmCommerceSearchable> filteredResult,
            IAddCommerceSearchFacets searchFacet, List<QueryStringFilter> queryFilter)
        {

            if (searchFacet.FacetType == FacetType.Term)
            {
                var filteredTermsFacet = filteredResult.Facets[searchFacet.Name] as TermsFacet;
                var termsFacet = baseResult.Facets[searchFacet.Name] as TermsFacet;
                if (termsFacet == null) return null;

                var facet = new FindFacet()
                {
                    Id = searchFacet.Property?.OwnerLink?.ID ?? 0,
                    ShowExpanded = searchFacet.ShowExpanded,
                    FacetType = FacetType.Term,
                    Name = termsFacet.Name,
                    Value = searchFacet.Term
                };

                if (queryFilter == null) return facet;

                var facetSelected = queryFilter.Any(i => i.Key == facet.Value);
                var otherFacetSelected = queryFilter.Any(i => i.Key != facet.Value);

                var tfList = new List<FindFacetTerm>();
                foreach (var tf in termsFacet.Terms)
                {
                    var baseCount = tf.Count;
                    var count = tf.Count;

                    if (facetSelected && queryFilter.Any(i => i.Value == tf.Term) || otherFacetSelected && !facetSelected)
                    {
                        count = filteredTermsFacet?.Terms.FirstOrDefault(i => i.Term == tf.Term)?.Count ?? 0;
                    }
                    if (baseCount == 0) { continue; }
                    {
                        var fftDisplayName = GetFftDisplayName(searchFacet.Term, tf.Term);
                        if (filteredTermsFacet.Name.ToLower().Contains("category"))
                        {
                            var excludeFromFilter = false;
                            var excludeFromFilters = filteredResult.Items.Where(x => (x as TrmVariant).Category == fftDisplayName);
                            if (excludeFromFilters != null && excludeFromFilters.Any())
                            {
                                foreach (var item in excludeFromFilters)
                                {
                                    if (_amVariantHelper.GetPrimaryCategory(item as TrmVariant) is TrmCategoryBase cat && cat.VisibleInLeftMenu == false) excludeFromFilter = true;
                                    break;
                                }
                            }
                            if (excludeFromFilter) continue;
                        }
                        if (!string.IsNullOrEmpty(fftDisplayName))
                        {
                            var fft = new FindFacetTerm
                            {
                                Selected = facetSelected && queryFilter.Any(i => i.Value == tf.Term),
                                Count = count,
                                Term = tf.Term,
                                DisplayName = fftDisplayName
                            };
                            tfList.Add(fft);
                        }
                    }
                }
                facet.Terms = tfList;
                return facet;
            }

            var filteredRangeFacet = filteredResult.Facets[searchFacet.Name] as NumericRangeFacet;
            var rangeFacet = baseResult.Facets[searchFacet.Name] as NumericRangeFacet;
            if (rangeFacet == null) return null;
            var rangefacet = new FindFacet()
            {
                Id = searchFacet.Property.OwnerLink.ID,
                ShowExpanded = searchFacet.ShowExpanded,
                FacetType = FacetType.Range,
                Name = rangeFacet.Name,
                Value = searchFacet.Term
            };

            if (queryFilter == null) return rangefacet;

            var rangefacetSelected = queryFilter.Any(i => i.Key == rangefacet.Value);
            var otherRangeFacetSelected = queryFilter.Any(i => i.Key != rangefacet.Value);

            foreach (var range in rangeFacet.Ranges)
            {
                //starting point
                var fromVal = !range.From.HasValue || range.From == double.MinValue
                    ? range.Min
                    : range.From;

                //ending point - either the max result for this range, or the natural end of the range
                var toVal = !range.To.HasValue || range.To == double.MaxValue
                    ? range.Max
                    : range.To;

                //how many results in this range
                var baseCount = range.Count;
                var count = range.Count;

                //deal with decimals
                var convertFromVal = Math.Round(Convert.ToDecimal(fromVal), 2);
                var convertToVal = Math.Round(Convert.ToDecimal(toVal), 2);

                if (rangefacetSelected && queryFilter.Any(i => i.Value == convertFromVal + "-" + convertToVal) || otherRangeFacetSelected && !rangefacetSelected)
                {
                    count = filteredRangeFacet?.Ranges.FirstOrDefault(r => r.From == range.From && r.To == range.To)?.Count ?? 0;
                }


                if (rangeFacet.Name.Equals(StringConstants.FacetOrderByPrice))
                {
                    //TODO: Currency is hard coded
                    var currency = "£";

                    //decide if we show the "search over XXX" or the "XXX - YYY" by checking if our "to" for this range is equal to the endpoint of all ranges (not max of current facet result - which it was previously).
                    var displayFormat = toVal == searchFacet.EndPoint;
                    var display = displayFormat ? _localizationService.GetStringByCulture(StringResources.SearchOver, StringConstants.TranslationFallback.SearchOver, ContentLanguage.PreferredCulture) + " " + currency + convertFromVal.ToString("#.00")
                        : string.Format("{0}{1} - {0}{2}", currency, convertFromVal.ToString("0.00"), ((double)convertToVal - 0.01).ToString("#.00"));

                    if (baseCount > 0 || queryFilter.Any(i => i.Value == convertFromVal + "-" + convertToVal))
                    {
                        rangefacet.Terms.Add(new FindFacetTerm()
                        {
                            Selected = rangefacetSelected && queryFilter.Any(i => i.Value == convertFromVal + "-" + convertToVal),
                            Count = count,
                            Term = convertFromVal + "-" + convertToVal,
                            DisplayName = display
                        });
                    }
                }
                else
                {
                    if (baseCount > 0 || queryFilter.Any(i => i.Value == convertFromVal + "-" + convertToVal))
                    {
                        rangefacet.Terms.Add(new FindFacetTerm()
                        {
                            Selected = rangefacetSelected && queryFilter.Any(i => i.Value == convertFromVal + "-" + convertToVal),
                            Count = count,
                            Term = fromVal + "-" + toVal,
                            DisplayName = fromVal + " - " + (toVal - 1)
                        });
                    }
                }
            }
            return rangefacet;
        }
        private string GetFftDisplayName(string searchFacetName, string tfTerm)
        {
            if (searchFacetName == StringConstants.FacetRankedDenomination)
            {
                var denominationRankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<Denomination>>();
                return denominationRankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetAlloy)
            {
                var alloyRankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<Alloy>>();
                return alloyRankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetPureMetal)
            {
                var alloyRankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<PureMetal>>();
                return alloyRankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetQuality)
            {
                var pureMetalPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<Quality>>();
                return pureMetalPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetContains)
            {
                var rankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<Contains>>();
                return rankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetDiamondQuality)
            {
                var rankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<DiamondQuality>>();
                return rankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetUKSize)
            {
                var rankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<UKSize>>();
                return rankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetMaterial)
            {
                var rankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<Material>>();
                return rankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetEuropeanSize)
            {
                var rankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<EuropeanSize>>();
                return rankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetColour)
            {
                var rankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<Colour>>();
                return rankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }
            if (searchFacetName == StringConstants.FacetCoinRange)
            {
                var rankedPropertyHelper = ServiceLocator.Current.GetInstance<IAmRankedPropertyHelper<CoinRange>>();
                return rankedPropertyHelper.GetRankedMultiSelectDisplayName(tfTerm);
            }

            return tfTerm;
        }
        public IList<EntryPartialViewModel> GetEntryViewModel(FindResults<IAmCommerceSearchable> results,
            EntrySortOrder entrySortOrder = EntrySortOrder.SortBy, Enums.eStockStatus[] excludeStatuses = null)
        {
            return GetEntryViewModel(results, false, entrySortOrder, excludeStatuses);
        }

        public IList<EntryPartialViewModel> GetEntryViewModel(FindResults<IAmCommerceSearchable> results,
            bool showEntryCompare, EntrySortOrder entrySortOrder, Enums.eStockStatus[] excludeStatuses = null)
        {
            var list = new List<EntryPartialViewModel>();
            var imageDisplaySizes =
                new Dictionary<string, int>
                {
                    {
                        StringConstants.Breakpoints.Lg, 262
                    },
                    {
                        StringConstants.Breakpoints.Md, 213
                    },
                    {
                        StringConstants.Breakpoints.Sm, 220
                    },
                    {
                        StringConstants.Breakpoints.Xs, 158
                    }
                };
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            var index = GetItemIndex(results);
            foreach (var result in results.Results)
            {
                index += 1;

                Price originalPrice = null;
                Price currentPrice = null;
                string originalPriceString = null;
                string currentPriceString = null;
                string currencyCode = null;
                decimal price = 0;

                var preciousMetalVariant = result as PreciousMetalsVariantBase;
                bool isPreciousMetalVariant = preciousMetalVariant != null;                

                if (isPreciousMetalVariant)
                {
                    Money physicalVariantPrice = _bullionPriceHelper.GetFromPriceForPhysicalVariant(preciousMetalVariant);
                    price = physicalVariantPrice.Amount;
                    currencyCode = physicalVariantPrice.Currency;
                    currentPriceString = physicalVariantPrice.ToString();                 
                }
                else
                {
                    var prices = _storeHelper.GetOriginalAndDiscountPrices(result.ContentLink, 1);
                    if (prices?.Count == 2)
                    {
                        originalPrice = prices[0];
                        currentPrice = prices[1];
                        price = currentPrice?.Money.Amount ?? 0;
                        currencyCode = currentPrice?.Money.Currency;

                        originalPriceString = originalPrice != null
                            ? _storeHelper.GetPriceAsString(originalPrice.Money.Amount, originalPrice.Money.Currency)
                            : null;
                        currentPriceString = currentPrice != null
                            ? _storeHelper.GetPriceAsString(currentPrice.Money.Amount, currentPrice.Money.Currency)
                            : null;
                    }
                }

                if (!_giftingHelper.CanShowContent(result.ContentLink))
                {
                    continue;
                }

                var entryPartialViewModelToAdd = new EntryPartialViewModel()
                {
                    ContentLink = result.ContentLink,
                    Title = result.Name,
                    EntryReference = result.ContentLink,
                    OriginalPriceString = originalPriceString,
                    PriceString = currentPriceString,
                    Price = price,
                    CurrencyCode = currencyCode,
                    DefaultImageUrl = _assetHelper.GetDefaultAssetUrl(result.ContentLink),
                    AltImageUrl = _assetHelper.GetAlternativeAssetUrl(result.ContentLink),
                    EntryUrl = result.ContentLink.GetExternalUrl_V2(),
                    CanBePersonalised = result.CanBePersonalised == Shared.Constants.Enums.CanBePersonalised.Yes,
                    InCompare = showEntryCompare,
                    ImageDisplaySizes = imageDisplaySizes,
                    DisableFeefo = startPage.PerformanceMode || string.IsNullOrWhiteSpace(startPage.FeefoReviewFeed),
                    IsPreciousMetalsVariant = isPreciousMetalVariant
                };
                var iAmEntryContentBase = result as EntryContentBase;
                if (iAmEntryContentBase != null)
                {
                    entryPartialViewModelToAdd.Code = iAmEntryContentBase.Code;
                    entryPartialViewModelToAdd.DisplayName = iAmEntryContentBase.DisplayName.IsNullOrEmpty()
                        ? result.Name
                        : iAmEntryContentBase.DisplayName;
                }

                entryPartialViewModelToAdd.YearOfIssue = int.Parse(string.IsNullOrWhiteSpace(result.YearOfIssue) ? "0" : result.YearOfIssue);
                entryPartialViewModelToAdd.YearOfWidthdrawal = int.Parse(string.IsNullOrWhiteSpace(result.YearOfWithdrawal) ? "0" : result.YearOfWithdrawal); ;
                var iHaveAShortDescription = result as IHaveAShortDescription;
                if (iHaveAShortDescription != null)
                {
                    entryPartialViewModelToAdd.Description = iHaveAShortDescription.Description;
                }
                var iHaveAProductSpecification = result as IHaveAProductSpecification;
                if (iHaveAProductSpecification != null)
                {
                    entryPartialViewModelToAdd.DenominationDisplayName =
                        iHaveAProductSpecification.RankedDenominationDisplayName;
                    entryPartialViewModelToAdd.ObverseDesigner = iHaveAProductSpecification.ObverseDesigner;
                }
                var iControlMyMerchandising = result as IControlMyMerchandising;
                if (iControlMyMerchandising != null)
                {
                    entryPartialViewModelToAdd.ReleaseDate = iControlMyMerchandising.ReleaseDate;
                }
                var iHaveATagMessage = result as IHaveATagMessage;
                if (iHaveATagMessage != null)
                {
                    entryPartialViewModelToAdd.TagMessage = iHaveATagMessage.TagMessage;
                }

                var iControlInventory = result as IControlInventory;
                if (iControlInventory != null)
                {
                    var stockSummary = _inventoryHelper.GetStockSummary(result.ContentLink);
                    entryPartialViewModelToAdd.StockSummary = stockSummary;
                }

                var trmVariant = result as TrmVariant;
                if (trmVariant != null)
                {
                    entryPartialViewModelToAdd.Badge = trmVariant.Badge;
                }

                var trmVariationBase = result as TrmVariationBase;
                if (trmVariationBase != null)
                {
                    entryPartialViewModelToAdd.SubDisplayName = trmVariationBase.SubDisplayName;
                    entryPartialViewModelToAdd.IsAgeRestricted = trmVariationBase.IsAgeRestricted;
                    entryPartialViewModelToAdd.IsPersonalised = trmVariationBase.IsPersonalised;
                    entryPartialViewModelToAdd.Quality = _specificationHelper.GetSpecificationItems(trmVariationBase).FirstOrDefault(x => x.Label == "Quality")?.Value;
                    entryPartialViewModelToAdd.PureMetalType = _specificationHelper.GetSpecificationItems(trmVariationBase).FirstOrDefault(x => x.Label == "Pure Metal Type")?.Value;

                    // TODO: Consider moving to helper
                    if ((entryPartialViewModelToAdd.Quality == "Bullion" || entryPartialViewModelToAdd.Quality == "Proof") && !string.IsNullOrEmpty(entryPartialViewModelToAdd.PureMetalType))
                    {
                        entryPartialViewModelToAdd.Standard = $"{entryPartialViewModelToAdd.PureMetalType} {entryPartialViewModelToAdd.Quality}";
                        entryPartialViewModelToAdd.StandardClass = $"{entryPartialViewModelToAdd.PureMetalType.ToLower()}-{entryPartialViewModelToAdd.Quality.ToLower()}";
                    }
                    if (entryPartialViewModelToAdd.Quality == "Brilliant Uncirculated")
                    {
                        entryPartialViewModelToAdd.Standard = "Brilliant Uncirculated Coin";
                        entryPartialViewModelToAdd.StandardClass = "brilliant-uncirculated";
                    }
                }

                var validViewModel = true;
                if (excludeStatuses != null)
                {
                    validViewModel = !excludeStatuses.Contains(entryPartialViewModelToAdd.StockSummary.Status);
                }

                entryPartialViewModelToAdd.VariantSchema = _analyticsDigitalData.GenerateVariantSchema(entryPartialViewModelToAdd, index);

                if (validViewModel) list.Add(entryPartialViewModelToAdd);
            }

            return list;
        }

        public IList<TeaserViewModel> GetArticleViewModel(IContentResult<IContentData> results)
        {
            var resultsAsArticles = new FindResults<IContent>();

            foreach (var findResult in results)
            {
                resultsAsArticles.Results.Add(findResult as IContent);
            }

            return GetArticleViewModel(resultsAsArticles);
        }

        public TeaserViewModel GetArticleViewModel(IContent content)
        {
            var teaser = _teaserHelper.GetTeaserDto(content);
            return new TeaserViewModel { Teaser = teaser, PageUrl = content.ContentLink.GetExternalUrl_V2() };
        }

        public IList<TeaserViewModel> GetArticleViewModel(FindResults<IContent> results)
        {
            var articleList = new List<TeaserViewModel>();

            foreach (var article in results.Results)
            {
                var teaser = _teaserHelper.GetTeaserDto(article);

                var teaserViewModel = new TeaserViewModel() { Teaser = teaser, PageUrl = article.ContentLink.GetExternalUrl_V2() };
                articleList.Add(teaserViewModel);

            }


            return articleList;

        }

        private int GetItemIndex(FindResults<IAmCommerceSearchable> results)
        {
            var index = 0;
            if (results == null) return index;
            return (results.CurrentPage - 1) * results.ItemsPerPage;
        }

    }
}