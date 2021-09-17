using System.ComponentModel.DataAnnotations;
using EPiServer;
using EPiServer.Core;
using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Framework.Localization;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.Web.Routing;

namespace TRM.Web.Business.VisitorGroups
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class ViewedContentCriterionModel : CriterionModelBase, IValidateCriterionModel
    {
        public ViewedContentCriterionModel()
            : base((LocalizationService)null)
        {
        }

        public ViewedContentCriterionModel(LocalizationService localizationService)
            : base(localizationService)
        {
        }

        public string Content { get; set; }

        public ContentReference GetContentReference()
        {
            if (string.IsNullOrWhiteSpace(this.Content))
            {
                return ContentReference.EmptyReference;
            }

            if (ContentReference.TryParse(this.Content, out ContentReference parsed))
            {
                return parsed;
            }

            var fromUrl = UrlResolver.Current.Route(new UrlBuilder(new UrlBuilder(this.Content).Path))?.ContentLink;
            if (fromUrl != null)
            {
                return fromUrl;
            }

            return ContentReference.EmptyReference;
        }


        public override ICriterionModel Copy()
        {
            ViewedContentCriterionModel viewedContentCriterionModel = (ViewedContentCriterionModel)this.ShallowCopy();
            return (ICriterionModel)viewedContentCriterionModel;
        }

        public CriterionValidationResult Validate(VisitorGroup currentGroup)
        {
            return new CriterionValidationResult(true);
        }
    }
}
