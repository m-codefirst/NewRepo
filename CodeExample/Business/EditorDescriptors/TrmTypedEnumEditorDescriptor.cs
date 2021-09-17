using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Framework.Localization;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using Hephaestus.CMS.Shell.ObjectEditing;

namespace TRM.Web.Business.EditorDescriptors
{
    public class TrmTypedEnumSelectionFactory<TEnum> : ISelectionFactory
    {
        private static readonly TypedEnumSelectionFactory<TEnum> TypedEnumSelectionFactory = new TypedEnumSelectionFactory<TEnum>();
        public virtual IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var selections = GetSelections(metadata.IsNullableValueType);
            var noDefaultValue = metadata.EditorConfiguration["NoDefaultValue"] as bool?;
            if (noDefaultValue.HasValue && noDefaultValue.Value)
            {
                return selections.Where(x => !string.IsNullOrEmpty(x.Text));
            }

            return selections;
        }

        public IEnumerable<ISelectItem> GetSelections(bool includeBlankValue)
        {
            if (includeBlankValue)
            {
                yield return (ISelectItem)new SelectItem
                {
                    Text = string.Empty
                };
            }
            Array values = Enum.GetValues(typeof(TEnum));
            foreach (object item in values)
            {
                yield return (ISelectItem)new SelectItem
                {
                    Text = GetValueName(item),
                    Value = (int)item
                };
            }
        }

        private static string GetValueName(object value)
        {
            string text = Enum.GetName(typeof(TEnum), value) ?? string.Empty;
            string resourceKey = $"/nvc/property/enum/{typeof(TEnum).Name.ToLowerInvariant()}/{text.ToLowerInvariant()}";
            return LocalizationService.Current.GetString(resourceKey, text);
        }
    }

    public class TrmTypedEnumEditorDescriptor<TEnum> : EditorDescriptor
    {
        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            base.SelectionFactoryType = typeof(TrmTypedEnumSelectionFactory<TEnum>);
            base.ClientEditingClass = "epi-cms/contentediting/editors/SelectionEditor";
            metadata.EditorConfiguration.Add("NoDefaultValue", true);
            base.ModifyMetadata(metadata, attributes);
        }
    }
}