using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Find.Commerce;
using EPiServer.Framework.Localization;
using EPiServer.Logging.Compatibility;
using Hephaestus.CMS.Extensions;
using Hephaestus.Commerce.Helpers;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.Interfaces.EntryProperties;
using TRM.Web.Models.ViewModels;
using TRM.Shared.Extensions;
using TRM.Web.Helpers.Interfaces;

namespace TRM.Web.Helpers
{
    public class EntryHelper : IAmEntryHelper
    {
        protected readonly ILog Logger = LogManager.GetLogger(typeof(EntryHelper));
        private readonly IContentLoader _contentLoader;
        private readonly IRelationRepository _relationRepository;
        private readonly IAssociationRepository _associationRepository;
        private readonly ReferenceConverter _referenceConverter;
        private readonly LocalizationService _localizationService;
        private readonly IAmAssetHelper _assetHelper;
        private readonly IAmTeaserHelper _teaserHelper;
        private readonly IAmStoreHelper _storeHelper;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly ICurrentMarket _currentMarket;
        private readonly IBullionPriceHelper _bullionPriceHelper;
        private readonly IAmSpecificationHelper _specificationHelper;
        private readonly IAnalyticsDigitalData _analyticsDigitalData;
        public EntryHelper(IContentLoader contentLoader,
                           IRelationRepository relationRepository,
                           ReferenceConverter referenceConverter,
                           LocalizationService localizationService, 
                           IAmAssetHelper assetHelper, 
                           IAmTeaserHelper teaserHelper, 
                           IAmInventoryHelper inventoryHelper, 
                           IAmStoreHelper storeHelper,
                           ICurrentMarket currentMarket, 
                           IAssociationRepository associationRepository,
                           IBullionPriceHelper bullionPriceHelper,
                           IAmSpecificationHelper specificationHelper,
                           IAnalyticsDigitalData analyticsDigitalData)
        {
            _contentLoader = contentLoader;
            _relationRepository = relationRepository;
            _referenceConverter = referenceConverter;
            _localizationService = localizationService;
            _assetHelper = assetHelper;
            _teaserHelper = teaserHelper;
            _storeHelper = storeHelper;
            _inventoryHelper = inventoryHelper;
            _currentMarket = currentMarket;
            _associationRepository = associationRepository;
            _bullionPriceHelper = bullionPriceHelper;
            _specificationHelper = specificationHelper;
            _analyticsDigitalData = analyticsDigitalData;
        }

        public string GetDisplayName(ContentReference contentReference)
        {
            if (contentReference.ID == 0) return string.Empty;

            var entry = _contentLoader.Get<EntryContentBase>(contentReference);

            if (entry == null) return string.Empty;

            var axProductVariant = entry as IAmAnAxProduct;
            return !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName :
                                        (!string.IsNullOrEmpty(entry.Name) ? entry.Name :
                                        (axProductVariant != null ? axProductVariant.AxProductTitle : entry.Code));
        }

        public string GetTruncatedDisplayName(ContentReference contentReference)
        {
            var displayName = GetDisplayName(contentReference);

            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();

            if (startPage == null || startPage.VariantTitleApplyEllipsisAfterCharacters <= 0 ||
                displayName.Length <= startPage.VariantTitleApplyEllipsisAfterCharacters) return displayName;

            var ellipsis = _localizationService.GetString(StringResources.Ellipsis, "...");
            var truncatedName = displayName.Substring(0, startPage.VariantTitleApplyEllipsisAfterCharacters);
            return truncatedName + ellipsis;
        }

        public ContentReference GetCategoryContentReference(ContentReference entryContentReference)
        {
            var nodeRelations = _relationRepository.GetParents<NodeEntryRelation>(entryContentReference).ToList();

            var primaryRelation = nodeRelations.FirstOrDefault(x => x.IsPrimary);

            if (primaryRelation != null)
            {
                var thisRelationContentReference = _referenceConverter.GetContentLink(primaryRelation.Parent.ID, CatalogContentType.CatalogNode, primaryRelation.Parent.WorkID);
                var nodeContentBase = _contentLoader.Get<NodeContentBase>(thisRelationContentReference);
                return nodeContentBase.ContentLink;
            }
            else
            {
                // will find the next ContentReference in case no primary category is assigned
                foreach (var nodeRelation in nodeRelations)
                {
                    var thisRelationContentReference = _referenceConverter.GetContentLink(nodeRelation.Parent.ID, CatalogContentType.CatalogNode, nodeRelation.Parent.WorkID);
                    var nodeContentBase = _contentLoader.Get<NodeContentBase>(thisRelationContentReference);
                    return nodeContentBase.ContentLink;
                }
            }
            
            return null;
        }

        public List<EntryPartialViewModel> GetAssociationsForViewModel(ContentReference sourceRef, string groupName, bool filterByCurrentMarket = false)
        {
            var listToSet = new List<EntryPartialViewModel>();
            var imageDisplaySizes =
                new Dictionary<string, int>
                {
                    {
                        StringConstants.Breakpoints.Lg, 295
                    },
                    {
                        StringConstants.Breakpoints.Md, 240
                    },
                    {
                        StringConstants.Breakpoints.Sm, 220
                    },
                    {
                        StringConstants.Breakpoints.Xs, 158
                    }
                };
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            var index = 0;
            foreach (var associatedReference in GetAssociatedReferencesForGroupName(sourceRef, groupName))
            {
                index += 1;
                var content = _contentLoader.Get<IContent>(associatedReference);
                var entryContentBase = content as EntryContentBase;

                if (entryContentBase == null)
                {
                    continue;
                }

                if (filterByCurrentMarket)
                {
                    var iControlTrmMarketFiltering = content as IControlTrmMarketFiltering;
                    if (iControlTrmMarketFiltering != null)
                    {
                        if (
                            iControlTrmMarketFiltering.UnavailableMarkets.Contains(
                                _currentMarket.GetCurrentMarket().MarketId.Value))
                        {
                            continue;
                        }
                    }
                }

                var merchandising = entryContentBase as IControlMyMerchandising;

                string priceString = null;
                string originalPriceString = null;

                var preciousMetalVariant = entryContentBase as PreciousMetalsVariantBase;
                bool isPreciousMetalVariant = preciousMetalVariant != null;

                if (merchandising != null && merchandising.Sellable)
                {
                    if (isPreciousMetalVariant)
                    {
                        priceString = _bullionPriceHelper.GetFromPriceForPhysicalVariant(preciousMetalVariant).ToString();
                    }
                    else
                    {
                        var prices = _storeHelper.GetOriginalAndDiscountPrices(entryContentBase.ContentLink, 1);
                        if (prices?.Count == 2)
                        {
                            originalPriceString = _storeHelper.GetPriceAsString(prices[0].Money.Amount, prices[0].Money.Currency);
                            priceString = _storeHelper.GetPriceAsString(prices[1].Money.Amount, prices[1].Money.Currency);
                        }
                    }
                }

                var stockSummary = _inventoryHelper.GetStockSummary(entryContentBase.ContentLink);

                var entryPartialViewModel = new EntryPartialViewModel
                {
                    DisplayName = GetTruncatedDisplayName(entryContentBase.ContentLink),
                    DefaultImageUrl = _assetHelper.GetPartialtAssetUrl(entryContentBase.ContentLink),
                    PriceString = priceString,
                    OriginalPriceString = originalPriceString,
                    EntryReference = entryContentBase.ContentLink,
                    EntryUrl = entryContentBase.ContentLink.GetExternalUrl_V2(),
                    AltImageUrl =  _assetHelper.GetAlternativeAssetUrl(entryContentBase.ContentLink),
                    TeaserDto = _teaserHelper.GetTeaserDto(entryContentBase),
                    Code = entryContentBase.Code,
                    ImageDisplaySizes = imageDisplaySizes,
                    StockSummary = stockSummary,
                    DisableFeefo = startPage.PerformanceMode || string.IsNullOrWhiteSpace(startPage.FeefoReviewFeed),
                    IsPreciousMetalsVariant = isPreciousMetalVariant
                };


                var trmVariationBase = entryContentBase as TrmVariationBase;
                if (trmVariationBase != null)
                {
                    entryPartialViewModel.SubDisplayName = trmVariationBase.SubDisplayName;
                    entryPartialViewModel.DenominationDisplayName = trmVariationBase.RankedDenominationDisplayName;
                    entryPartialViewModel.IsAgeRestricted = trmVariationBase.IsAgeRestricted;
                    entryPartialViewModel.IsPersonalised = trmVariationBase.IsPersonalised;
                    entryPartialViewModel.TagMessage = trmVariationBase.TagMessage;
                    entryPartialViewModel.Quality = _specificationHelper.GetSpecificationItems(trmVariationBase).FirstOrDefault(x => x.Label == "Quality")?.Value;
                    entryPartialViewModel.PureMetalType = _specificationHelper.GetSpecificationItems(trmVariationBase).FirstOrDefault(x => x.Label == "Pure Metal Type")?.Value;
                    
                    // TODO: Consider moving to helper
                    if ((entryPartialViewModel.Quality == "Bullion" || entryPartialViewModel.Quality == "Proof") && !string.IsNullOrEmpty(entryPartialViewModel.PureMetalType))
                    {
                        entryPartialViewModel.Standard = $"{entryPartialViewModel.PureMetalType} {entryPartialViewModel.Quality}";
                        entryPartialViewModel.StandardClass = $"{entryPartialViewModel.PureMetalType.ToLower()}-{entryPartialViewModel.Quality.ToLower()}";
                    }
                    if (entryPartialViewModel.Quality == "Brilliant Uncirculated")
                    {
                        entryPartialViewModel.Standard = "Brilliant Uncirculated Coin";
                        entryPartialViewModel.StandardClass = "brilliant-uncirculated";
                    }
                }

                entryPartialViewModel.VariantSchema = _analyticsDigitalData.GenerateVariantSchema(entryPartialViewModel, index);
                listToSet.Add(entryPartialViewModel);
            }
            return listToSet;
        }

        public IEnumerable<ContentReference> GetAssociatedReferencesForGroupName(ContentReference reference, string groupName)
        {
            var associations = new List<Association>();

            try
            {
                associations =
                    _associationRepository.GetAssociations(reference).Where(a => a.Group.Name == groupName).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Error whilst gathering the group references. : {0}", ex.Message));
            }

            return !associations.Any()
                ? new List<ContentReference>()
                : associations.Select(associated => associated.Target).ToList();
        }

        public IEnumerable<int> GetAssociatedReferences(ContentReference reference)
        {
            var associations = new List<Association>();

            try
            {
                associations =
                    _associationRepository.GetAssociations(reference).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Error whilst gathering the group references. : {0}", ex.Message));
            }

            return !associations.Any()
                ? new List<int>()
                : associations.Select(associated => associated.Target.ID);
        }

        public string GetTagMessage(ContentReference contentReference)
        {
            var content = _contentLoader.Get<EntryContentBase>(contentReference);

            var iControlMyTagMessage = content as IHaveATagMessage;
            if (iControlMyTagMessage == null) return string.Empty;
            return iControlMyTagMessage.TagMessage ?? string.Empty;
        }
        
        public List<int> GetAllCategoryContentReferenceIds(ContentReference entryContentReference)
        {
            return GetAllCategoryContentReferences(entryContentReference).Select(x => x.ID).ToList();
        }

        public List<ContentReference> GetAllCategoryContentReferences(ContentReference entryContentReference)
        {
            var nodeRelations = _relationRepository.GetParents<NodeRelation>(entryContentReference);
            if (nodeRelations == null) return null;

            var references = new List<ContentReference>();
            foreach (var nodeRelation in nodeRelations)
            {
                var catalogContentBase = _contentLoader.Get<CatalogContentBase>(nodeRelation.Parent);
                var nodeContentBase = catalogContentBase as NodeContentBase;
                if (nodeContentBase == null) continue;
                references.Add(nodeContentBase.ContentLink);
            }
            return references;
        }

        public Dictionary<string, decimal> GetPrices(ContentReference content)
        {
            var prices = new Dictionary<string, decimal>();

            if (content.ID == 0) return prices;

            var variant = _contentLoader.Get<TrmVariationBase>(content);

            if (variant == null) return prices;
            var physicalVariant = variant as PhysicalVariantBase;
            if (physicalVariant != null)
            {
                foreach (var price in physicalVariant.Prices())
                {
                    var physicalVariantPrice = _bullionPriceHelper.GetFromPriceForPhysicalVariant(physicalVariant).Amount;
                    prices.Add(price.MarketId.Value + "_", physicalVariantPrice);
                }
                return prices;
            }

            foreach (var price in variant.Prices())
            {
                prices.Add(price.MarketId.Value + "_" + price.CustomerPricing.PriceCode, Math.Round(price.UnitPrice.Amount,2));
            }

            return prices;
        }

        public TrmVariant GetVariantFromCode(string code)
        {
            var entryReference = _referenceConverter.GetContentLink(code);
            if (entryReference == null || entryReference.ID == 0) return null;

            var entry = _contentLoader.Get<TrmVariant>(entryReference);
            return entry;
        }
    }
}