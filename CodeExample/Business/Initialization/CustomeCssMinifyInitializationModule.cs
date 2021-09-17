using System.IO;
using System.Text.RegularExpressions;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Blobs;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class CustomeCssMinifyInitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            //Add initialization logic, this method is called once after CMS has been initialized
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            contentEvents.PublishingContent += contentEvents_PublishingContent;
        }

        public void Preload(string[] parameters) { }

        public void Uninitialize(InitializationEngine context)
        {
            //Add uninitialization logic
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            contentEvents.PublishingContent -= contentEvents_PublishingContent;
        }

        private void contentEvents_PublishingContent(object sender, ContentEventArgs e)
        {
            var customeCssFile = e.Content as MediaData;
            if (customeCssFile == null || !customeCssFile.Name.Equals(Constants.StringConstants.CssCustomerFileName)) return;

            var content = System.Text.Encoding.UTF8.GetString(customeCssFile.BinaryData.ReadAllBytes());
            var minifiedContent = RemoveWhiteSpaceFromStylesheets(content);
            //Create new blob
            UpdateFileContent(customeCssFile, minifiedContent);
        }

        private void UpdateFileContent(MediaData mediaData, string replaceContent)
        {
            if (mediaData == null) return;

            var blobFactory = ServiceLocator.Current.GetInstance<IBlobFactory>();
            mediaData.BinaryData = blobFactory.CreateBlob(mediaData.BinaryDataContainer, ".css");
            using (var s = mediaData.BinaryData.OpenWrite())
            {
                var w = new StreamWriter(s);
                w.WriteLine(replaceContent);
                w.Flush();
            }
        }

        private static string RemoveWhiteSpaceFromStylesheets(string body)
        {
            body = Regex.Replace(body, @"[a-zA-Z]+#", "#");
            body = Regex.Replace(body, @"[\n\r]+\s*", string.Empty);
            body = Regex.Replace(body, @"\s+", " ");
            body = Regex.Replace(body, @"\s?([:,;{}])\s?", "$1");
            body = body.Replace(";}", "}");
            body = Regex.Replace(body, @"([\s:]0)(px|pt|%|em)", "$1");
            // Remove comments from CSS
            body = Regex.Replace(body, @"/\*[\d\D]*?\*/", string.Empty);

            return body;
        }
    }
}