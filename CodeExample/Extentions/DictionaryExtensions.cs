using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRM.Web.Models.DDS;

namespace TRM.Web.Extentions
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, string> CastToDictionary(this IEnumerable<Country> countries)
        {
            if (countries == null || !countries.Any()) return new Dictionary<string, string>();
            return countries.ToDictionary(c => c.CountryCode, c => c.CountryName);
        }

        public static Dictionary<string, string> CastToDictionary(this IEnumerable<Currency> currencies)
        {
            if (currencies == null || !currencies.Any()) return new Dictionary<string, string>();
            return currencies.ToDictionary(c => c.Value, c => c.DisplayName);
        }
        /// <summary>
        /// Add item with check existing key for dictionary
        /// </summary>
        public static void TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if(dic == null) dic = new Dictionary<TKey, TValue>();
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, value);
            }
            else
            {
                dic[key] = value;
            }
        }

        public static void TryAddIfNotExist<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (dic == null) dic = new Dictionary<TKey, TValue>();
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, value);
            }            
        }

        public static TValue TryGet<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key)
        {
            if (dic == null) dic = new Dictionary<TKey, TValue>();
            return dic.ContainsKey(key) ? dic[key] : default(TValue);
        }
    }
}