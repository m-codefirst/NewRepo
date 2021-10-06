using EPiServer;
using EPiServer.DataAccess;
using System.IO;
using TRM.Web.Services.MerchandiseFeed.Models;
using TRM.Web.Services.MerchandiseFeed.Pages;

namespace TRM.Web.Services.MerchandiseFeed
{
    public class FeedGeneration : IGenerateFeed
    {
        private readonly FeedBuilder _builder;
        private readonly IContentRepository _contentRepository;

        public FeedGeneration(FeedBuilder builder, IContentRepository contentRepository)
        {
            _builder = builder;
            _contentRepository = contentRepository;
        }

        public void Generate(FeedPage feedPage)
        {
            var feed = _builder.Build();
            var output = string.Empty;

            using (var stream = new MemoryStream())
            {
                var formatter = new NamespacedXmlMediaTypeFormatter();
                var content = new System.Net.Http.StreamContent(stream);
                // Serialize the object.
                formatter.WriteToStreamAsync(feed.GetType(), feed, stream, content, null).Wait();
                // Read the serialized string.
                stream.Position = 0;
                output = content.ReadAsStringAsync().Result;
            }

            var clone = (FeedPage)feedPage.CreateWritableClone();
            clone.Output = output;
            _contentRepository.Save(clone, SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);
        }
    }
}