using EPiServer.Find.Helpers;
using Hephaestus.ContentTypes.Business.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using TRM.Web.Constants;

namespace TRM.Web.Extentions
{
    public static class EnumExtensions
    {
        public static List<string> GetGenderList()
        {
            var genders = Enum.GetValues(typeof(Enums.eGenders));

            var genderList = new List<string>
            {
                "Select"
            };

            foreach (var title in genders)
            {
                genderList.Add(title.DescriptionAttr());
            }

            return genderList;
        }

        public static string GetEnumDescriptionAttrWithFallback(Enum e, string fallback)
        {
            return Convert.ToInt32(e) > 0 ? e.DescriptionAttr() : fallback;
        }

        public static string GetEnumDescriptionAttrWithFallback(Enum e, Enum fallback)
        {
            return Convert.ToInt32(e) > 0 ? e.DescriptionAttr() : fallback.DescriptionAttr();
        }

        public static Dictionary<int, string> ToDictionary<T>() where T : Enum
        {
            var enumValues = Enum.GetValues(typeof(T)).Cast<T>();

            return enumValues
                .Select(value => new { id = Convert.ToInt32(value), Text = value.DescriptionAttr() })
                .ToDictionary(x => x.id, x => x.Text);
        }

        public static IEnumerable<SelectListItem> ToSelectListItems<T>() where T : Enum
        {
            var enumValues = Enum.GetValues(typeof(T)).Cast<T>();

            return enumValues
                .Select(value => new SelectListItem {Value = Convert.ToInt32(value).ToString(), Text = value.DescriptionAttr()});
        }
    }
}