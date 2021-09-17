using System.Collections.Generic;
using EPiServer.Core;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public interface IAmEntryHelper
    {
        string GetDisplayName(ContentReference contentReference);
        string GetTruncatedDisplayName(ContentReference entryContentReference);
        ContentReference GetCategoryContentReference(ContentReference entryContentReference);
        List<EntryPartialViewModel> GetAssociationsForViewModel(ContentReference sourceRef, string groupName, bool FilterByCurrentMarket);
        IEnumerable<ContentReference> GetAssociatedReferencesForGroupName(ContentReference reference, string groupName);
        IEnumerable<int> GetAssociatedReferences(ContentReference reference);
        string GetTagMessage(ContentReference contentReference);
        List<int> GetAllCategoryContentReferenceIds(ContentReference entryContentReference);
        List<ContentReference> GetAllCategoryContentReferences(ContentReference entryContentReference);
        Dictionary<string, decimal> GetPrices(ContentReference content);
        TrmVariant GetVariantFromCode(string code);
    }
}