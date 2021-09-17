using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using EPiServer.Web;
using Hephaestus.CMS.Extensions;
using TRM.Web.Extentions;
using TRM.Web.Models.Pages;
using TRM.Shared.Extensions;

namespace TRM.Web.Helpers
{
    public class AssetHelper : IAmAssetHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IBlobFactory _blobFactory;

        public AssetHelper(IContentLoader contentLoader, IBlobFactory blobFactory)
        {
            _contentLoader = contentLoader;
            _blobFactory = blobFactory;
        }

        public string GetNoPictureImageUrl()
        {
            var noImageProperty = GetNoPictureUrlFromStartPage();
            return noImageProperty != null ? noImageProperty.GetExternalUrl_V2() : "/Static/img/global/noimage.png";
        }

        public string GetTrmNoPictureImageUrl()
        {
            var noImageProperty = GetNoPictureUrlFromStartPage();
            return noImageProperty != null ? noImageProperty.GetExternalUrl_V2() : "/Static/img/global/trm-no-image.png";
        }

        private ContentReference GetNoPictureUrlFromStartPage()
        {
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null) return null;
           return startPage.NoPictureIcon;
        }

        public string GetDefaultAssetUrl(ContentReference contentReference)
        {
            if (contentReference == null)
                return GetNoPictureImageUrl();

            var groupName = "Default";
            
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage != null) groupName = startPage.DefaultAssetImageGroupName;
        
            return GetCatalogReferenceAssetUrl(contentReference, groupName, GetNoPictureImageUrl());
        }

        public string GetAlternativeAssetUrl(ContentReference contentReference)
        {
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null) return string.Empty;

            var groupName = startPage.AlternativeAssetImageGroupName;

            return GetCatalogReferenceAssetUrl(contentReference, groupName, string.Empty);
        }

        public List<string> GetEntryAssetUrls(EntryContentBase entry)
        {
            var assets = entry.CommerceMediaCollection.OrderBy(x => x.SortOrder);
            var list = assets.Select(x => x.AssetLink.GetExternalUrl_V2()).ToList();
            return list;
        }

        public List<string> GetEntryAssetUrlsWithGroupName(EntryContentBase entry, string groupName)
        {
            var assets = entry.CommerceMediaCollection.Where(x => !string.IsNullOrEmpty(groupName) && x.GroupName == groupName).OrderBy(x => x.SortOrder);
            var list = assets.Select(x => x.AssetLink.GetExternalUrl_V2()).ToList();
            return list;
        }

        public string GetCatalogReferenceAssetUrl(ContentReference contentReference, string groupName, string fallback)
        {
            var catalogContent = _contentLoader.Get<CatalogContentBase>(contentReference) as IAssetContainer;
            if (catalogContent == null) return fallback;
            var defaultAsset = catalogContent.CommerceMediaCollection.Where(x => x.GroupName.Equals(groupName))
                .OrderBy(x => x.SortOrder)
                .FirstOrDefault();

            return defaultAsset != null ? defaultAsset.AssetLink.GetExternalUrl_V2() : fallback;
        }

		public string GetPartialtAssetUrl(ContentReference contentReference)
		{
			var groupName = "Partial";

			var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
			if (startPage != null) groupName = startPage.PartialImageGroupName;

			return GetCatalogReferenceAssetUrl(contentReference, groupName, null) ?? GetDefaultAssetUrl(contentReference);
		}

        public Blob GetSavedPrintzwareImage(string fileName)
        {
            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;
            if (startPage == null || string.IsNullOrWhiteSpace(startPage.PersonalisationContainerId)) return null;

            Guid containerGuid;
            if (!Guid.TryParse(startPage.PersonalisationContainerId, out containerGuid)) return null;

            var imageUri = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}://{1}/{2}/{3}",
                Blob.BlobUriScheme,
                Blob.DefaultProvider,
                containerGuid.ToString("N").ToLowerInvariant(),
                fileName));

            return _blobFactory.GetBlob(imageUri);
        }
    }
}
