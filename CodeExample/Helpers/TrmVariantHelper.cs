using EPiServer;
using EPiServer.AddOns.Helpers;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Logging.Compatibility;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Hephaestus.CMS.ViewModels;
using Hephaestus.Commerce.Helpers;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRM.Shared.Extensions;
using TRM.Web.Business.Cart;
using TRM.Web.Constants;
using TRM.Web.Controllers.Blocks;
using TRM.Web.Extentions;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Interfaces.EntryProperties;
using TRM.Web.Models.Layouts;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels;
using TRM.Web.Services;

namespace TRM.Web.Helpers
{
    public class TrmVariantHelper : ITrmVariantHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IAmAssetHelper _assetHelper;
        private readonly IAmTeaserHelper _teaserHelper;
        private readonly IBullionPriceHelper _bullionPriceHelper;
        private readonly IAmCurrencyHelper _currencyHelper;
        private readonly IAmSpecificationHelper _specificationHelper;
        private readonly IAmStoreHelper _storeHelper;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly IAmEntryHelper _entryHelper;
        private readonly ITrmCartService _cartService;
        private readonly IAmCartHelper _cartHelper;
        private readonly IBackInStockService _backInStockService;
        private readonly IAmLayoutHelper _layoutHelper;
        private readonly IAmNavigationHelper _navigationHelper;
        private readonly IAmRecentlyViewedHelper _recentlyViewedHelper;
        private readonly IPrintzwareHelper _printzwareHelper;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly IAmVariantHelper _variantHelper;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IUserService _userService;
        private readonly IAnalyticsDigitalData _analyticsDigitalData;
        private readonly UrlResolver _urlResolver;
        private readonly LocalizationService _localizationService;

        private const string Bullion = "Bullion";
        private const string Proof = "Proof";
        private const string BrilliantUncirculated = "Brilliant Uncirculated";
        private const string BrilliantUncirculatedCoin = "Brilliant Uncirculated Coin";
        private const string Brilliant_Uncirculated = "brilliant-uncirculated";

        private readonly ILog _logger = LogManager.GetLogger(typeof(VariantCarouselBlockController));

        public TrmVariantHelper(
            IContentLoader contentLoader,
            IAmAssetHelper assetHelper,
            IAmTeaserHelper teaserHelper,
            IBullionPriceHelper bullionPriceHelper,
            IAmCurrencyHelper currencyHelper,
            IAmSpecificationHelper specificationHelper,
            IAmStoreHelper storeHelper,
            IAmInventoryHelper inventoryHelper,
            IAmEntryHelper entryHelper,
            ITrmCartService cartService,
            IAmCartHelper cartHelper,
            IBackInStockService backInStockService,
            IAmLayoutHelper layoutHelper,
            IAmNavigationHelper navigationHelper,
            IAmRecentlyViewedHelper recentlyViewedHelper,
            IPrintzwareHelper printzwareHelper,
            ISiteDefinitionResolver siteDefinitionResolver,
            IAmVariantHelper variantHelper,
            IAmBullionContactHelper bullionContactHelper,
            IUserService userService,
            IAnalyticsDigitalData analyticsDigitalData,
            UrlResolver urlResolver,
            LocalizationService localizationService)
        {
            _contentLoader = contentLoader;
            _assetHelper = assetHelper;
            _teaserHelper = teaserHelper;
            _bullionPriceHelper = bullionPriceHelper;
            _currencyHelper = currencyHelper;
            _specificationHelper = specificationHelper;
            _storeHelper = storeHelper;
            _inventoryHelper = inventoryHelper;
            _entryHelper = entryHelper;
            _cartService = cartService;
            _cartHelper = cartHelper;
            _backInStockService = backInStockService;
            _layoutHelper = layoutHelper;
            _navigationHelper = navigationHelper;
            _recentlyViewedHelper = recentlyViewedHelper;
            _printzwareHelper = printzwareHelper;
            _siteDefinitionResolver = siteDefinitionResolver;
            _variantHelper = variantHelper;
            _bullionContactHelper = bullionContactHelper;
            _userService = userService;
            _analyticsDigitalData = analyticsDigitalData;
            _urlResolver = urlResolver;
            _localizationService = localizationService;
        }
        public IPageViewModel<TrmVariant, ILayoutModel, EntryPartialViewModel> CreatePageViewModel(
            IPageViewModel<TrmVariant, ILayoutModel, EntryPartialViewModel> model, TrmVariant trmVariant)
        {
            model.ViewModel.AltImageUrl = _assetHelper.GetAlternativeAssetUrl(trmVariant.ContentLink);
            model.ViewModel.TeaserDto = _teaserHelper.GetTeaserDto(trmVariant);
            model.ViewModel.ImageDisplaySizes = ImageDispalySizes(580, 442, 553, 442);

            model.ViewModel.DenominationDisplayName = trmVariant.RankedDenominationDisplayName;
            model.ViewModel.SubDisplayName = trmVariant.SubDisplayName;
            model.ViewModel.IsAgeRestricted = trmVariant.IsAgeRestricted;
            model.ViewModel.IsPersonalised = trmVariant.IsPersonalised;
            model.ViewModel.TagMessage = trmVariant.TagMessage;
            model.ViewModel.Quality = _specificationHelper.GetSpecificationItems(trmVariant).FirstOrDefault(x => x.Label == "Quality")?.Value;
            model.ViewModel.PureMetalType = _specificationHelper.GetSpecificationItems(trmVariant).FirstOrDefault(x => x.Label == "Pure Metal Type")?.Value;
            model.ViewModel.Badge = trmVariant.Badge;

            var qualityStandard = UpdateQualityStandard(model.ViewModel.Quality, model.ViewModel.PureMetalType);
            model.ViewModel.Standard = qualityStandard != null ? qualityStandard.Standard : string.Empty;
            model.ViewModel.StandardClass = qualityStandard != null ? qualityStandard.StandardClass : string.Empty;

            var trmPhysicalVariant = trmVariant as PreciousMetalsVariantBase;
            if (trmPhysicalVariant != null)
            {
                model.ViewModel.PriceString = _bullionPriceHelper.GetFromPriceForPhysicalVariant(trmPhysicalVariant).ToString();
                model.ViewModel.IsPreciousMetalsVariant = true;
            }

            var trmSignatureVariant = trmVariant as SignatureVariant;
            if (trmSignatureVariant != null)
            {
                model.ViewModel.IsSignatureOnlyVariant = true;
                model.ViewModel.SignatureLandingPageUrl = trmSignatureVariant.GetBullionSignatureLandingPage();
                var currencyCode = _currencyHelper.GetDefaultCurrencyCode();
                var minimumPurchaseValue = trmSignatureVariant.MinSpendConfigs.OrderBy(x => x.Amount).FirstOrDefault(x => x.Currency.Equals(currencyCode))?.Amount ?? decimal.Zero;
                model.ViewModel.MinimumPurchaseFormatted = new Money(minimumPurchaseValue, currencyCode).ToString();
            }

            model.ViewModel.VariantSchema = _analyticsDigitalData.GenerateVariantSchema(model.ViewModel);

            return model;
        }
        
        public IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> CreatePageViewModel(
            IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> model, TrmVariant trmVariant, VariantViewModel viewModel, StartPage startPage, HttpRequestBase request)
        {
            if (!string.IsNullOrEmpty(startPage?.SecondaryAssetImageGroupName))
                model.ViewModel.Images = _assetHelper.GetEntryAssetUrlsWithGroupName(trmVariant, startPage.SecondaryAssetImageGroupName);

            if (!string.IsNullOrEmpty(startPage?.Asset360ImageGroupName))
            {
                model.ViewModel.ImagesFor360 = _assetHelper.GetEntryAssetUrlsWithGroupName(trmVariant, startPage.Asset360ImageGroupName);
                model.ViewModel.Icon360 = startPage?.Variant360Icon.GetExternalUrl_V2();
                model.ViewModel.Icon360Overlay = startPage?.Variant360IconOverlay.GetExternalUrl_V2();
            }

            var fulFilledResourceKey = GetFulfilledBy(trmVariant.FulfilledBy);
            model.ViewModel.FulfilledBy = _localizationService.GetString(fulFilledResourceKey);
            model.ViewModel.ShowCompare = startPage != null && startPage.ShowCompareOnEntryDetails;
            model.ViewModel.ShowFeefo = startPage != null && !startPage.PerformanceMode && !string.IsNullOrWhiteSpace(startPage.FeefoReviewFeed);
            model.ViewModel.NoFeefoReviewsMessage = startPage != null ? startPage.NoFeefoReviewsMessage : new ContentArea();
            model.ViewModel.HideAddToWishList = startPage != null && startPage.HideAddToWishList;
            model.ViewModel.IsItemExistingInWishList = _cartHelper.IsItemExistingInCart(trmVariant.Code, _cartService.DefaultWishListName);
            model.ViewModel.Icon360 = startPage?.Variant360Icon.GetExternalUrl_V2();
            model.ViewModel.Icon360Overlay = startPage?.Variant360IconOverlay.GetExternalUrl_V2();

            model.ViewModel.ShouldShowEmailBackInStockButton = ShouldShowEmailBackInStockButton(model.ViewModel, trmVariant, startPage);
            SetPropertiesForEmailBackInStock(model.ViewModel, startPage);

            var trmLayoutModel = model.Layout as TrmLayoutModel;
            if (trmLayoutModel == null) return model;

            trmLayoutModel.IsInvestmentPage = trmVariant.IsInvestmentPage;

            if (trmLayoutModel.EnableCustomerCss) trmLayoutModel.CustomerCssUrl = _layoutHelper.RetrieveGlobalCustomerStylesheet();

            _navigationHelper.CreateBreadcrumb(trmLayoutModel, trmVariant);

            if (!trmLayoutModel.UseManualMegaMenu || trmLayoutModel.IsPensionContact)
            {
                _navigationHelper.CreateMegaMenu(trmLayoutModel);
            }

            _navigationHelper.CreateMyAccountHoverMenu(trmLayoutModel);

            if (startPage != null)
            {
                trmLayoutModel.ShowSocialShareOnSellableEntryDetails =
                    trmVariant.Sellable && startPage.ShowSocialShareOnSellableEntryDetails &&
                    !string.IsNullOrEmpty(trmLayoutModel.SocialSharesSnippetOnEntryDetails);
                trmLayoutModel.ShowSocialShareOnSellableEntryDetails =
                    !trmVariant.Sellable && startPage.ShowSocialShareOnNonSellableEntryDetails &&
                    !string.IsNullOrEmpty(trmLayoutModel.SocialSharesSnippetOnEntryDetails);
                trmLayoutModel.SocialSharesSnippetOnEntryDetails = trmLayoutModel.SocialSharesSnippetOnEntryDetails;
                trmLayoutModel.HideUserAndMiniBasketFromHeader = startPage.SiteWideHideUserAndMiniBasketFromHeader;
            }


            var icontrolLeftNav = trmVariant as IControlLeftNav;
            if (icontrolLeftNav.ShowMyLeftMenu && icontrolLeftNav.ShowAutomaticLeftNavigation)
            {
                _navigationHelper.CreateAutomaticLeftMenu(trmLayoutModel, trmVariant);
            }
            if (icontrolLeftNav.ShowMyLeftMenu && icontrolLeftNav.ShowManualLeftNavigation &&
                icontrolLeftNav.LeftMenuManualContentArea != null &&
                icontrolLeftNav.LeftMenuManualContentArea.FilteredItems != null &&
                icontrolLeftNav.LeftMenuManualContentArea.FilteredItems.Any())
            {
                _navigationHelper.CreateManualLeftMenu(trmLayoutModel, icontrolLeftNav);
            }
            viewModel.BulletPoints = new List<string>
            {
                trmVariant.BulletPoint1,
                trmVariant.BulletPoint2,
                trmVariant.BulletPoint3,
                trmVariant.BulletPoint4,
                trmVariant.BulletPoint5,
                trmVariant.BulletPoint6,
                trmVariant.BulletPoint7
            }.Where(x => !string.IsNullOrWhiteSpace(x));

            if (!string.IsNullOrEmpty(startPage.RelatedEntriesGroupName) &&
                ((trmVariant.Sellable && startPage.ShowRelatedEntriesOnSellableEntryPages) ||
                 (trmVariant.Sellable && startPage.ShowRelatedEntriesOnNonSellableEntryPages)))
            {
                viewModel.RelatedEntries = _entryHelper.GetAssociationsForViewModel(trmVariant.ContentLink,
                    startPage.RelatedEntriesGroupName, true);
            }

            if (trmVariant.PublishOntoSite && trmVariant.Sellable)
            {
                _recentlyViewedHelper.SetRecentlyViewedCookie(trmVariant.Code);
            }

            viewModel.EntrySpecificationViewModel = new SpecificationTableBlockViewModel
            {
                SpecificationTableName = trmVariant.SpecificationTableName,
                SpecificationItems = _specificationHelper.GetSpecificationItems(trmVariant)
            };

            //Personalisation
            SetPropertiesForPersonalisation(viewModel, startPage, trmVariant);

            model.Layout.CanonicalUrl = trmVariant.CanonicalUrl != null ?
               trmVariant.CanonicalUrl.GetContentReference()?.GetFriendlyCanonicalUrl(trmVariant.Language, _urlResolver, _siteDefinitionResolver, request) :
               _variantHelper.GetCanonicalUrl(trmVariant, request);

            ModifyLayout(trmLayoutModel, trmVariant);

            model.ViewModel.DenominationDisplayName = trmVariant.RankedDenominationDisplayName;
            model.ViewModel.TagMessage = trmVariant.TagMessage;
            model.ViewModel.Quality = trmVariant.Quality;
            model.ViewModel.PureMetalType = trmVariant.PureMetalType;

            var qualityStandard = UpdateQualityStandard(model.ViewModel.Quality, model.ViewModel.PureMetalType);
            model.ViewModel.Standard = qualityStandard != null ? qualityStandard.Standard : string.Empty;
            model.ViewModel.StandardClass = qualityStandard != null ? qualityStandard.StandardClass : string.Empty;

            model.ViewModel.Badge = trmVariant.Badge;

            return model;
        }

        public VariantCarouselBlockViewModel CreatePageViewModel(VariantCarouselBlock currentBlock)
        {
            if (currentBlock.VariantCarouselContentArea == null || !(currentBlock.VariantCarouselContentArea.Items?.Count > 0)) return null;

            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as Models.Pages.StartPage;
            if (startPage == null)
            {
                _logger.Error("Start Page has not been found. As such unable to obtain Variant Information");
                throw new Exception();
            }

            var model = new VariantCarouselBlockViewModel();
            var listToSet = new List<EntryPartialViewModel>();
            var index = 0;
            foreach (var items in currentBlock.VariantCarouselContentArea.Items)
            {
                index += 1;
                IContent content;
                if (!_contentLoader.TryGet<IContent>(items.ContentLink, out content)) continue;

                var entryContentBase = content as EntryContentBase;
                if (entryContentBase == null) continue;

                var merchandising = entryContentBase as IControlMyMerchandising;
                if (merchandising != null && (!merchandising.PublishOntoSite || merchandising.Restricted)) continue;

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

                var item = new EntryPartialViewModel
                {
                    DisplayName = _entryHelper.GetTruncatedDisplayName(entryContentBase.ContentLink),
                    DefaultImageUrl = _assetHelper.GetPartialtAssetUrl(entryContentBase.ContentLink),
                    PriceString = priceString,
                    OriginalPriceString = originalPriceString,
                    EntryReference = entryContentBase.ContentLink,
                    EntryUrl = entryContentBase.ContentLink.GetExternalUrl_V2(),
                    AltImageUrl = _assetHelper.GetAlternativeAssetUrl(entryContentBase.ContentLink),
                    TeaserDto = _teaserHelper.GetTeaserDto(entryContentBase),
                    Code = entryContentBase.Code,
                    ImageDisplaySizes = ImageDispalySizes(295, 240, 220, 158),
                    StockSummary = _inventoryHelper.GetStockSummary(entryContentBase.ContentLink),
                    DisableFeefo = startPage.PerformanceMode || string.IsNullOrWhiteSpace(startPage.FeefoReviewFeed),
                    IsPreciousMetalsVariant = isPreciousMetalVariant

                };
                var trmVariationBase = entryContentBase as TrmVariationBase;
                if (trmVariationBase != null)
                {
                    item.SubDisplayName = trmVariationBase.SubDisplayName;
                    item.DenominationDisplayName = trmVariationBase.RankedDenominationDisplayName;
                    item.IsAgeRestricted = trmVariationBase.IsAgeRestricted;
                    item.IsPersonalised = trmVariationBase.IsPersonalised;
                    var trmVariant = trmVariationBase as TrmVariant;
                    item.CanBePersonalised = trmVariant != null && trmVariant.CanBePersonalised == TRM.Shared.Constants.Enums.CanBePersonalised.Yes;
                    item.Badge = trmVariant?.Badge;
                    item.TagMessage = trmVariationBase.TagMessage;
                    item.Quality = _specificationHelper.GetSpecificationItems(trmVariationBase).FirstOrDefault(x => x.Label == "Quality")?.Value;
                    item.PureMetalType = _specificationHelper.GetSpecificationItems(trmVariationBase).FirstOrDefault(x => x.Label == "Pure Metal Type")?.Value;

                    var qualityStandard = UpdateQualityStandard(item.Quality, item.PureMetalType);
                    item.Standard = qualityStandard != null ? qualityStandard.Standard : string.Empty;
                    item.StandardClass = qualityStandard != null ? qualityStandard.StandardClass : string.Empty;
                }

                var trmSignatureVariant = entryContentBase as SignatureVariant;
                if (trmSignatureVariant != null)
                {
                    item.IsSignatureOnlyVariant = true;
                    item.SignatureLandingPageUrl = startPage.BullionSignatureLandingPage.GetExternalUrl_V2();
                    var currencyCode = _currencyHelper.GetDefaultCurrencyCode();
                    var minimumPurchaseValue = trmSignatureVariant.MinSpendConfigs.OrderBy(x => x.Amount).FirstOrDefault(x => x.Currency.Equals(currencyCode))?.Amount ?? decimal.Zero;
                    item.MinimumPurchaseFormatted = new Money(minimumPurchaseValue, currencyCode).ToString();
                }

                item.VariantSchema = _analyticsDigitalData.GenerateVariantSchema(item, index);

                listToSet.Add(item);
            }
            model.Item = listToSet;
            model.VariantCarouselContentArea = currentBlock.VariantCarouselContentArea;
            model.VariantCarouselHeadingContentArea = currentBlock.VariantCarouselHeadingContentArea;
            model.ItemsToDisplay = currentBlock.ItemsToDisplay;

            return model;
        }
        public void ModifyLayout(ILayoutModel layoutModel, IContent catalogContent)
        {
            var trmLayoutModel = layoutModel as ITrmLayoutModel;
            if (trmLayoutModel == null) return;

            trmLayoutModel.AnnouncementContentArea = _layoutHelper.GetAnnouncementBannerContentArea(catalogContent);
            trmLayoutModel.DefaultCurrencySymbol = _currencyHelper.GetDefaultCurrencySymbol();
            trmLayoutModel.DefaultCurrencyCode = _currencyHelper.GetDefaultCurrencyCode();

            var currentContact = CustomerContext.Current.CurrentContact;
            trmLayoutModel.CustomerIsBullionAccount = _bullionContactHelper.IsBullionAccount(currentContact);
            trmLayoutModel.IsSippContact = _bullionContactHelper.IsSippContact(currentContact);
            trmLayoutModel.IsPensionProviderContact = _bullionContactHelper.IsPensionProvider(currentContact);
            trmLayoutModel.IsConsumerAccountOnly = _bullionContactHelper.IsConsumerAccountOnly(currentContact);

            trmLayoutModel.KycRefered = _bullionContactHelper.KycRefered(currentContact);
            trmLayoutModel.KycRejected = _bullionContactHelper.KycRejected(currentContact);
            trmLayoutModel.IsCustomerServiceAccount = _bullionContactHelper.IsCustomerServiceAccount(currentContact);
            trmLayoutModel.IsImpersonating = _userService.IsImpersonating();

            var accountBeforeImpersonating = _userService.GetContactBeforeImpersonating();
            if (accountBeforeImpersonating != null)
                trmLayoutModel.IsImpertionatedByPensionProvider = _bullionContactHelper.IsPensionProvider(accountBeforeImpersonating);
        }
        public Dictionary<string, int> ImageDispalySizes(int lg, int md, int sm, int xs)
        {
            return new Dictionary<string, int>
                {
                    { StringConstants.Breakpoints.Lg, lg },
                    { StringConstants.Breakpoints.Md, md },
                    { StringConstants.Breakpoints.Sm, sm },
                    { StringConstants.Breakpoints.Xs, xs }
                };
        }
        public QualityStandardDto UpdateQualityStandard(string quality, string pureMetalType)
        {
            var qualityStandardDto = new QualityStandardDto();
            if (!string.IsNullOrWhiteSpace(quality) && !string.IsNullOrWhiteSpace(pureMetalType) && (quality == Bullion || quality == Proof))
            {
                qualityStandardDto.Standard = $"{pureMetalType} {quality}";
                qualityStandardDto.StandardClass = $"{pureMetalType.ToLower()}-{quality.ToLower()}";

            }
            else if (quality == BrilliantUncirculated)
            {
                qualityStandardDto.Standard = BrilliantUncirculatedCoin;
                qualityStandardDto.StandardClass = Brilliant_Uncirculated;
            }

            return qualityStandardDto;
        }

        private string GetFulfilledBy(string input)
        {
            return $"{StringResources.FulfilledBy}/{input}".ToLowerInvariant();
        }
        private bool ShouldShowEmailBackInStockButton(VariantViewModel viewModel, TrmVariant trmVariant, Models.Pages.StartPage startPage)
        {
            if (trmVariant == null || startPage == null) return false;

            if (viewModel.StockSummary == null || viewModel.StockSummary.NotAvailableForEwbis) return false;

            return _backInStockService.CanSignupEmailForVariant(trmVariant);
        }
        private void SetPropertiesForEmailBackInStock(VariantViewModel viewModel, Models.Pages.StartPage startPage)
        {
            if (viewModel == null || startPage == null) return;

            string emailMeWhenInStockButtonText;
            string emailAddressEntryHeading;
            XhtmlString emailAddressEntryContent;
            string cancelButtonText;
            string continueButtonText;
            if (startPage.EmailBackInStockSettingsPage != null)
            {
                var emailBackInStockSetting = _contentLoader.Get<EmailBackInStockSettingsPage>(startPage.EmailBackInStockSettingsPage);
                emailMeWhenInStockButtonText = emailBackInStockSetting.EmailMeWhenInStockButtonText;
                emailAddressEntryHeading = emailBackInStockSetting.EmailAddressEntryHeading;
                emailAddressEntryContent = emailBackInStockSetting.EmailAddressEntryContent;
                cancelButtonText = emailBackInStockSetting.CancelButtonText;
                continueButtonText = emailBackInStockSetting.ContinueButtonText;
            }
            else
            {
                emailMeWhenInStockButtonText = startPage.EmailMeWhenInStockButtonText;
                emailAddressEntryHeading = startPage.EmailAddressEntryHeading;
                emailAddressEntryContent = startPage.EmailAddressEntryContent;
                cancelButtonText = startPage.CancelButtonText;
                continueButtonText = startPage.ContinueButtonText;
            }

            viewModel.EmailMeWhenInStockButtonText = emailMeWhenInStockButtonText ??
                                                     _localizationService.GetStringByCulture(
                                                         StringResources.EmailMeWhenInStockButtonText,
                                                         StringConstants.TranslationFallback.EmailMeWhenInStockButtonText,
                                                         ContentLanguage.PreferredCulture);

            viewModel.EmailAddressEntryHeading = emailAddressEntryHeading ??
                                                 _localizationService.GetStringByCulture(
                                                     StringResources.EmailAddressEntryHeading,
                                                     StringConstants.TranslationFallback.EmailAddressEntryHeading,
                                                     ContentLanguage.PreferredCulture);

            viewModel.EmailAddressEntryContent = string.Format(emailAddressEntryContent?.ToString() ??
                                                               _localizationService.GetStringByCulture(
                                                                   StringResources.EmailAddressEntryContent,
                                                                   StringConstants.TranslationFallback.EmailAddressEntryContent,
                                                                   ContentLanguage.PreferredCulture), viewModel.DisplayName);

            viewModel.CancelButtonText = cancelButtonText ??
                                         _localizationService.GetStringByCulture(
                                             StringResources.CancelButtonText,
                                             StringConstants.TranslationFallback.CancelButtonText,
                                             ContentLanguage.PreferredCulture);

            viewModel.ContinueButtonText = continueButtonText ??
                                           _localizationService.GetStringByCulture(
                                               StringResources.ContinueButtonText,
                                               StringConstants.TranslationFallback.ContinueButtonText,
                                               ContentLanguage.PreferredCulture);
        }
        private void SetPropertiesForPersonalisation(VariantViewModel viewModel, Models.Pages.StartPage startPage, TrmVariant trmVariant)
        {
            viewModel.CanBePersonalised = trmVariant.CanBePersonalised.Equals(Shared.Constants.Enums.CanBePersonalised.Yes);
            if (!viewModel.CanBePersonalised) return;

            viewModel.PersonalisationMeMessage = $"{_localizationService.GetString(StringResources.PersonaliseMe, StringConstants.TranslationFallback.PersonaliseMe)} (+£{trmVariant.PersonalisationPrice})";
            viewModel.EditPersonalisationMessage = _localizationService.GetString(StringResources.EditYourPersonalised, StringConstants.TranslationFallback.EditYourPersonalised);
            viewModel.SessionTimeoutMessage = _localizationService.GetStringByCulture(StringResources.GiftingSessionTimeoutMessage, StringConstants.TranslationFallback.GiftingSessionTimeout, System.Globalization.CultureInfo.CurrentCulture);
            viewModel.PersonalisationType = trmVariant.PersonalisationType.ToString();

            viewModel.PrintzwareApiUrl = startPage.PrintzwareApiUrl;
            viewModel.PersonalisedCharge = trmVariant.PersonalisationPrice;

            viewModel.PersonalisedItemNumber = trmVariant.PersonalisedItemNumber;
            viewModel.PrintzwareVariantId = trmVariant.PrintzwareVariantId;
            var orderId = _printzwareHelper.GetUniqueId(trmVariant.PrintzwareVariantId);
            viewModel.PWOrderId = orderId;
            viewModel.PrintzwareOpenUrl = _printzwareHelper.OpenUrl(orderId, trmVariant.PrintzwareVariantId, false);
        }
    }
}