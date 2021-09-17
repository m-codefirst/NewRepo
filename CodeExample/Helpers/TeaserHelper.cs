using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using Hephaestus.CMS.Extensions;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces;
using TRM.Shared.Extensions;

namespace TRM.Web.Helpers
{
    public class TeaserHelper : IAmTeaserHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IAmAssetHelper _assetHelper;

        public TeaserHelper(IContentLoader contentLoader, IAmAssetHelper assetHelper)
        {
            _contentLoader = contentLoader;
            _assetHelper = assetHelper;
        }

        public TeaserDto GetTeaserDto(IContent currentContent)
        {
            var thisTeaser = new TeaserDto();
            var myContentData = _contentLoader.Get<ContentData>(currentContent.ContentLink);
            if (myContentData == null) return thisTeaser;

            var iControlTeaser = myContentData as IControlTeaser;
            if (iControlTeaser == null)
            {
                return thisTeaser;
            }

            #region cmsTeaser

            if (myContentData is PageData || myContentData is BlockData)
            {
                var iControlPageHeading = myContentData as IControlPageHeader;

                thisTeaser.TeaserTitle = !string.IsNullOrWhiteSpace(iControlTeaser.TeaserBlock.TeaserTitle)
                    ? iControlTeaser.TeaserBlock.TeaserTitle
                    : (iControlPageHeading != null && !string.IsNullOrWhiteSpace(iControlPageHeading.Heading)
                        ? iControlPageHeading.Heading
                        : currentContent.Name);

                thisTeaser.TeaserDescription = !string.IsNullOrWhiteSpace(iControlTeaser.TeaserBlock.TeaserDescription)
                    ? iControlTeaser.TeaserBlock.TeaserDescription
                    : string.Empty;
                thisTeaser .TeaserImageUrl = GetTeaserImageUrl(currentContent);
                thisTeaser.ButtonText = iControlTeaser.TeaserBlock.ButtonText;
                thisTeaser.ButtonStyle = iControlTeaser.TeaserBlock.ButtonStyle;

                return thisTeaser;
            }

            #endregion

            #region commerceTeaser

            if (!(myContentData is CatalogContentBase)) return thisTeaser;

            //need to try cast as both - DisplayName isn't on a nice interface :'(
            var nodeContent = myContentData as NodeContent;
            var entryContentBase = myContentData as EntryContentBase;
            var displayName = entryContentBase != null
                ? entryContentBase.DisplayName
                : (nodeContent != null ? nodeContent.DisplayName : currentContent.Name);
            thisTeaser.TeaserTitle = !string.IsNullOrWhiteSpace(iControlTeaser.TeaserBlock.TeaserTitle)
                ? iControlTeaser.TeaserBlock.TeaserTitle
                : displayName;

            thisTeaser.TeaserDescription = !string.IsNullOrWhiteSpace(iControlTeaser.TeaserBlock.TeaserDescription)
                ? iControlTeaser.TeaserBlock.TeaserDescription
                : string.Empty;

            thisTeaser.TeaserImageUrl = GetTeaserImageUrl(currentContent);
            thisTeaser.ButtonText = iControlTeaser.TeaserBlock.ButtonText;
            thisTeaser.ButtonStyle = iControlTeaser.TeaserBlock.ButtonStyle;

            #endregion

            return thisTeaser;
        }

        public string GetTeaserImageUrl(IContent currentContentLink)
        {
            var currentContentData = _contentLoader.Get<ContentData>(currentContentLink.ContentLink);

            var iControlTeaser = currentContentData as IControlTeaser;
            if (iControlTeaser == null) return string.Empty;

            var teaserImageUrl = iControlTeaser.TeaserBlock.TeaserImage.GetExternalUrl_V2();
            if (currentContentData is PageData || currentContentData is BlockData)
            {
                return teaserImageUrl;
            }
            if (currentContentData is CatalogContentBase)
            {
                return string.IsNullOrEmpty(teaserImageUrl) ? _assetHelper.GetDefaultAssetUrl(currentContentLink.ContentLink) : teaserImageUrl;
            }
            return string.Empty;
        }
    }
}