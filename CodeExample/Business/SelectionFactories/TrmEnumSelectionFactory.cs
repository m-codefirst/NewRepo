using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors.SelectionFactories;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using System;
using System.ComponentModel;
using System.Linq;
using EPiServer.Globalization;
using TRM.Web.Constants;

namespace TRM.Web.Business.SelectionFactories
{
    public class TrmEnumSelectionFactory<TEnum> : EnumSelectionFactory where TEnum : struct
    {
        public TrmEnumSelectionFactory() : this(ServiceLocator.Current.GetInstance<LocalizationService>())
        {
        }

        public TrmEnumSelectionFactory(LocalizationService localizationService) : base(localizationService)
        {
        }

        protected override Type EnumType => typeof(TEnum);

        protected override string GetStringForEnumValue(int value)
        {
            var stringValue = LocalizationService.GetStringByCulture($"{StringResources.TrmEnum}{this.EnumType.Name}/{Enum.GetName(this.EnumType, value)}", ContentLanguage.PreferredCulture);
            if (string.IsNullOrEmpty(stringValue))
            {
                var descAttr = EnumType.GetMember(EnumType.GetEnumName(value))
                        .FirstOrDefault()?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                        .FirstOrDefault() as DescriptionAttribute;
                stringValue = descAttr?.Description ?? Enum.GetName(EnumType, value);
            }

            return stringValue;

        }
    }
}