using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging;
using Mediachase.Commerce.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Web.Models.Catalog;
using TRM.Web.Services.MerchandiseFeed.Models;

namespace TRM.Web.Services.MerchandiseFeed
{
    public abstract class DefaultFeedBuilderBase : FeedBuilder
    {
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLanguageAccessor _languageAccessor;
        private readonly ILogger _logger;

        public DefaultFeedBuilderBase(IContentLoader contentLoader, ReferenceConverter referenceConverter, IContentLanguageAccessor languageAccessor)
        {
            _contentLoader = contentLoader;
            _referenceConverter = referenceConverter;
            _languageAccessor = languageAccessor;
            _logger = LogManager.GetLogger(typeof(DefaultFeedBuilderBase));
        }

        public override Feed Build()
        {
            _logger.Information("Building");

            var feed = GenerateFeedEntity() ?? throw new ArgumentNullException($"{nameof(GenerateFeedEntity)} returned null");
            var catalogReferences = _contentLoader.GetDescendents(_referenceConverter.GetRootLink());
            var items = _contentLoader.GetItems(catalogReferences, CreateDefaultLoadOption()).OfType<TrmVariant>();

            var entries = new List<Entry>();
            foreach (CatalogContentBase catalogContent in items)
            {
                try
                {
                    var entry = GenerateEntry(catalogContent);

                    if (entry != null)
                    {
                        _logger.Information($"Adding entry {entry.Id} to feed");

                        entries.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to generate Feed entry for ContentGuid={catalogContent.ContentGuid}", ex);
                }
            }

            feed.Channel.Entries = entries;

            return feed;
        }

        protected abstract Feed GenerateFeedEntity();

        protected abstract Entry GenerateEntry(CatalogContentBase catalogContent);

        private LoaderOptions CreateDefaultLoadOption()
        {
            LoaderOptions loaderOptions = new LoaderOptions
            {
                LanguageLoaderOption.FallbackWithMaster(_languageAccessor.Language)
            };

            return loaderOptions;
        }
    }
}