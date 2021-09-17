using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TRM.Web.Business.SelectionFactories
{
    public class TrmContentTypeSelectionFactory<TContentType> : ISelectionFactory
    {
        private readonly Lazy<IContentTypeRepository> ContentTypeRepository =
            new Lazy<IContentTypeRepository>(() =>
            {
                return ServiceLocator.Current.GetInstance<IContentTypeRepository>();
            });

        public TrmContentTypeSelectionFactory() { }

        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var result = new List<SelectItem>();
            var matchContentTypes = ContentTypeRepository.Value.List()
                .Where(x =>
                    x.ModelType != null &&
                    (typeof(TContentType).IsAssignableFrom(x.ModelType)
                    || typeof(TContentType).IsInstanceOfType(x.ModelType)));

            if (matchContentTypes == null)
            {
                return result;
            }

            foreach (var t in matchContentTypes)
            {
                result.Add(new SelectItem
                {
                    Text = $"{t.DisplayName}",
                    Value = t.ID.ToString()
                });
            }

            return result;
        }
    }
}