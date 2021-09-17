using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using TRM.Web.Constants;

namespace TRM.Web.Business.SelectionFactories
{
    public class TrmExtendEnumSelectionFactory<TEnum> : ISelectionFactory
    {
        protected Type EnumType => typeof(TEnum);
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var list = new List<ISelectItem>
            {
                new SelectItem
                {
                    Value = int.MaxValue,
                    Text = "None"
                }
            };
            list.AddRange(from int value in Enum.GetValues(EnumType)
                select new SelectItem
                {
                    Value = value,
                    Text = GetStringForEnumValue(value)
                });
            return list;
        }

        protected string GetStringForEnumValue(int value)
        {
            var localizationService = ServiceLocator.Current.GetInstance<LocalizationService>();
            var stringValue = localizationService.GetStringByCulture($"{StringResources.TrmEnum}{this.EnumType.Name}/{Enum.GetName(this.EnumType, value)}", ContentLanguage.PreferredCulture);
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