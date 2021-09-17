using System.Collections.Generic;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Framework.Blobs;

namespace TRM.Web.Helpers
{
    public interface IAmAssetHelper
    {
        string GetNoPictureImageUrl();
        string GetTrmNoPictureImageUrl();
        string GetDefaultAssetUrl(ContentReference contentReference);
        string GetAlternativeAssetUrl(ContentReference contentReference);
        List<string> GetEntryAssetUrls(EntryContentBase entry);
        List<string> GetEntryAssetUrlsWithGroupName(EntryContentBase entry, string groupName);
        string GetCatalogReferenceAssetUrl(ContentReference contentReference, string groupName, string fallback);
		string GetPartialtAssetUrl(ContentReference contentReference);
        Blob GetSavedPrintzwareImage(string fileName);
    }
}
