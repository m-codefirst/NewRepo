using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.Web;
using Mediachase.Commerce.Catalog;
using System;
using System.Linq;
using TRM.Shared.Extensions;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog;
using TRM.Web.Services.MerchandiseFeed.Models;

namespace TRM.Web.Services.MerchandiseFeed
{
    public class EpiDefaultFeedBuilder : DefaultFeedBuilderBase
    {
        private readonly IAmAssetHelper _assetHelper;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly ILogger _logger;

        private readonly string _siteUrl;

        public EpiDefaultFeedBuilder(
            IContentLoader contentLoader,
            ReferenceConverter referenceConverter,
            IContentLanguageAccessor languageAccessor,
            ISiteDefinitionRepository siteDefinitionRepository,
            IAmAssetHelper assetHelper,
            IAmInventoryHelper inventoryHelper)
            : base(contentLoader, referenceConverter, languageAccessor)
        {
            _assetHelper = assetHelper;
            _inventoryHelper = inventoryHelper;

            _logger = LogManager.GetLogger(typeof(EpiDefaultFeedBuilder));

            _siteUrl = siteDefinitionRepository.List().FirstOrDefault()?.SiteUrl.ToString();
        }

        protected override Feed GenerateFeedEntity()
        {
            _logger.Information("Generating feed");

            return new Feed
            {
                Channel = new Channel
                {
                    Updated = DateTime.UtcNow,
                    Title = "The Royal Mint Products",
                    Link = "http://www.royalmint.com/"
                }
            };
        }

        protected override Entry GenerateEntry(CatalogContentBase catalogContent)
        {
            var trmVariant = catalogContent as TrmVariant;

            if (trmVariant == null) return null;

            _logger.Information("Generating entry");

            var id = trmVariant.Code;
            var title = trmVariant.DisplayName;
            var description = trmVariant.Description?.ToHtmlString();
            var price = trmVariant.PriceForProductFeed;
            var gtin = trmVariant.GTIN;

            var availability = _inventoryHelper.GetStockSummary(trmVariant?.ContentLink)?.StatusMessage;

            switch (availability)
            {
                case "In Stock":
                    availability = "in stock";
                    break;
                default:
                    availability = "out of stock";
                    break;
            }

            var variantLink = trmVariant.ContentLink?.GetExternalUrl_V2();
            variantLink = Uri.TryCreate(new Uri(_siteUrl), variantLink, out var variantUri) ? variantUri.ToString() : variantLink;

            var imageLink = _assetHelper.GetDefaultAssetUrl(trmVariant.ContentLink);
            imageLink = Uri.TryCreate(new Uri(_siteUrl), imageLink, out var imageUri) ? imageUri.ToString() : imageLink;

            if (string.IsNullOrEmpty(id) ||
                string.IsNullOrEmpty(title) ||
                string.IsNullOrEmpty(description) ||
                string.IsNullOrEmpty(variantLink) ||
                string.IsNullOrEmpty(availability) ||
                string.IsNullOrEmpty(gtin) ||
                price == null)
                return null;

            _logger.Information("Variant is valid");

            return new Entry
            {
                Id = id,
                Title = title,
                Description = description,
                Link = variantLink,
                ImageLink = imageLink,
                Condition = "new",
                Availability = availability,
                Price = $"{price.Amount:N2} {price.Currency}",
                Brand = "The Royal Mint",
                GTIN = gtin,
            };
        }
    }
}