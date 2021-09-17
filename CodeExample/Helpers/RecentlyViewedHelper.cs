using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Web;
using Hephaestus.CMS.Extensions;
using Mediachase.Commerce.Catalog;
using TRM.Web.Constants;
using TRM.Web.Helpers.Find;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Interfaces.EntryProperties;
using TRM.Web.Models.Pages;
using TRM.Shared.Extensions;

namespace TRM.Web.Helpers
{
    public class RecentlyViewedHelper : IAmRecentlyViewedHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IAmAssetHelper _assetHelper;
        private readonly IAmEntryHelper _entryHelper;
        private readonly IGiftingHelper _giftingHelper;

        public RecentlyViewedHelper(IContentLoader contentLoader, ReferenceConverter referenceConverter, IAmEntryHelper entryHelper, IAmAssetHelper assetHelper, IGiftingHelper giftingHelper)
        {
            _contentLoader = contentLoader;
            _referenceConverter = referenceConverter;
            _entryHelper = entryHelper;
            _assetHelper = assetHelper;
            _giftingHelper = giftingHelper;
        }

        public List<RecentlyViewedDto> GetRecentlyViewedListProducts(string currentvariant)
        {
            var listToSetRecentlyViewedProducts = new List<RecentlyViewedDto>();
            var recentlyViewedCookie = CookieHelper.GetBasicCookie(StringConstants.Cookies.RecentlyViewed);

            if (recentlyViewedCookie?.Value == null)
            {
                return listToSetRecentlyViewedProducts;
            }
            var recentlyViewedList = recentlyViewedCookie.Value.Split(',').ToList();


            foreach (var sku in recentlyViewedList.Where(w => !string.IsNullOrEmpty(w)))
            {
                if (sku.Equals(currentvariant))
                {
                    continue;
                }
                var entryReference = _referenceConverter.GetContentLink(sku);
                if (entryReference == null || entryReference.ID == 0) continue;

                var entry = _contentLoader.Get<CatalogContentBase>(entryReference);
                if (entry == null) continue;
                var merchandising = entry as IControlMyMerchandising;
                if (merchandising != null && (!merchandising.Sellable || !merchandising.PublishOntoSite))
                {
                    continue;
                }

                if (!_giftingHelper.CanShowContent(entry.ContentLink))
                {
                    continue;
                }
               
                listToSetRecentlyViewedProducts.Add(new RecentlyViewedDto
                {
                    Code = sku,
                    Name = _entryHelper.GetTruncatedDisplayName(entry.ContentLink),
                    DefaultImageUrl = _assetHelper.GetDefaultAssetUrl(entry.ContentLink),
                    EntryUrl = entry.ContentLink.GetExternalUrl_V2()
                });

            }
            
            return listToSetRecentlyViewedProducts;

        }


        public void SetRecentlyViewedCookie(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return;

            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;

            if (!(startPage?.RecentlyViewedLimit > 0)) return;

            var recentlyViewedCookie = CookieHelper.GetBasicCookie(StringConstants.Cookies.RecentlyViewed);
            var recentlyViewedList = new List<string>();

            if (recentlyViewedCookie?.Value != null)
            {
                recentlyViewedList = recentlyViewedCookie.Value.Split(',').ToList();
            }

            if (recentlyViewedList.Contains(sku)) return;

            recentlyViewedList.Add(sku);

            if (recentlyViewedList.Count > startPage.RecentlyViewedLimit)
            {
                recentlyViewedList.RemoveAt(0);
            }

            CookieHelper.CreateBasicCookie(StringConstants.Cookies.RecentlyViewed, string.Join(",", recentlyViewedList));
        }
    }
}