using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Web;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Helpers
{
    public class VariantHelper : IAmVariantHelper
    {
        private readonly IRelationRepository _relationRepository;
        private readonly IContentLoader _contentLoader;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        

        public VariantHelper(IRelationRepository relationRepository, IContentLoader contentLoader, ISiteDefinitionResolver siteDefinitionResolver)
        {
            _relationRepository = relationRepository;
            _contentLoader = contentLoader;
            _siteDefinitionResolver = siteDefinitionResolver;
        }

        public NodeContent GetPrimaryCategory(VariationContent variant)
        {
            var parents = _relationRepository.GetParents<NodeRelation>(variant.ContentLink).ToList();
            var nodeRelation = parents.FirstOrDefault(x => (x as NodeEntryRelation)?.IsPrimary ?? false) ?? parents.FirstOrDefault();
            if (nodeRelation == null) return null;

            return _contentLoader.Get<CatalogContentBase>(nodeRelation.Parent) as TrmCategory;
        }

        public Url GetCanonicalUrl(VariationContent variant, HttpRequestBase request)
        {
            TrmCategory primaryCategory = GetPrimaryCategory(variant) as TrmCategory;
            if (primaryCategory == null)
            {
                return string.Empty;
            }

            var categories = _contentLoader.GetAncestors(primaryCategory.ContentLink).OfType<TrmCategory>().Reverse();

            var url = new StringBuilder();
            foreach (var category in categories)
            {
                url.Append(category.RouteSegment).Append("/");
            }

            url.Append(primaryCategory.RouteSegment);

            var siteDefinition = _siteDefinitionResolver.Get(request);
            if (siteDefinition == null) return string.Empty;

            var myPrimaryHost = siteDefinition.GetPrimaryHost(variant.Language);
            if (myPrimaryHost == null) return string.Empty;

            return myPrimaryHost.Url + url.ToString() + $"/{variant.RouteSegment}/";
        }
    }
}