using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EPiServer.Framework.Localization;
using EPiServer.Shell.ObjectEditing;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [ExcludeFromCodeCoverage]
    public class EnumSelectionFactory<TEnum> : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(
            ExtendedMetadata metadata)
        {
            var values = Enum.GetValues(typeof(TEnum));
            foreach (var value in values)
            {
                yield return new SelectItem
                {
                    Text = GetValueName(value),
                    Value = value
                };
            }
        }

        public string GetValueName(object value)
        {
            var attributes = value.GetType().GetField(value.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), true);

            var staticName = Enum.GetName(typeof(TEnum), value);

            if (string.IsNullOrEmpty(staticName)) return staticName;

            var localizationPath = string.Format("/property/enum/{0}/{1}", typeof(TEnum).Name.ToLowerInvariant(), staticName.ToLowerInvariant());

            if (LocalizationService.Current.TryGetString(
                localizationPath,
                out var localizedName))
            {
                return localizedName;
            }
            else if (attributes != null && attributes.Any())
            {
                var att = (DescriptionAttribute)attributes.First();
                return att.Description;
            }

            return staticName;
        }
    }
}
