using System;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;

namespace Hephaestus.Commerce.Extensions
{
    public static class ContentReferenceExtensions
    {
        private static IContentRepository _contentRepository;

        public static IContentRepository ContentRepository
        {
            get
            {
                return _contentRepository ??
                       (_contentRepository =
                           ServiceLocator.Current.GetInstance<IContentRepository>());
            }
            set { _contentRepository = value; }
        }

        public static T GetCatalogueNode<T>(this ContentReference contentReference, bool eatAllExceptions)
            where T : NodeContent
        {
            if (!eatAllExceptions) return GetCatalogueNode<T>(contentReference);
            try
            {
                return GetCatalogueNode<T>(contentReference);
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetLogger(typeof (ContentReferenceExtensions));
                logger.Error(
                    string.Format("Failed to retrieve a Catalogue Node of Type {1} for ContentReference.ID={0}.",
                        contentReference.ID, 
                        typeof (T)), 
                    ex);
            }
            return null;
        }

        public static T GetCatalogueNode<T>(this ContentReference contentReference) where T : NodeContent
        {
            return ContentRepository.Get<T>(contentReference);
        }
    }
}