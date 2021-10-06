using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRM.Web.Services.Metapack.Models.Request
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class QueryStringAttribute : Attribute
    {
        public string Name { get; set; }

        public QueryStringAttribute()
        {
        }
    }

    public static class IQueryStringRequestPartExtension
    {
        public static string ToQueryString(this IQueryStringRequestPart part)
        {
            var values =
                from p in part.GetType().GetProperties()
                let att = Attribute.GetCustomAttribute(p, typeof(QueryStringAttribute)) as QueryStringAttribute
                let name = att?.Name ?? p.Name
                let val = p.GetValue(part, null)
                where val != null
                select name + "=" + HttpUtility.UrlEncode(val.ToString());

            return String.Join("&", values.ToArray());
        }
    }

    public interface IQueryStringRequestPart
    {
    }

    public class ShippingRequest : IQueryStringRequestPart
    {
        [QueryString(Name = "key")]
        public Guid Key { get; set; }

        [QueryString(Name = "wh_code")]
        public string WarehouseCode { get; set; }
        [QueryString(Name = "wh_pc")]
        public string WarehousePostcode { get; set; }
        [QueryString(Name = "wh_cc")]
        public string WarehouseCountryCode { get; set; }

        [QueryString(Name = "c_pc")]
        public string CustomerPostcode { get; set; }
        [QueryString(Name = "c_cc")]
        public string CustomerCountryCode { get; set; }

        [QueryString(Name = "parcelDepth")]
        public string ParcelDepth { get; set; }
        [QueryString(Name = "parcelHeight")]
        public string ParcelHeight { get; set; }
        [QueryString(Name = "parcelWidth")]
        public string ParcelWidth { get; set; }
        [QueryString(Name = "parcelWeight")]
        public string ParcelWeight { get; set; }

        [QueryString(Name = "dimensions_unit")]
        public string DimensionsUnit { get; set; }
        [QueryString(Name = "weight_unit")]
        public string UnitWeight { get; set; }
        [QueryString(Name = "e_w")]
        public decimal? EstimatedWeight { get; set; }
        [QueryString(Name = "e_v")]
        public decimal? TotalValue { get; set; }

        [QueryString(Name = "r_t")]
        public string ReturnType { get; set; }
        [QueryString(Name = "r_f")]
        public string ReturnFormat { get; set; }
        [QueryString(Name = "optionType")]
        public string OptionType { get; set; }

        [QueryString(Name = "limit")]
        public int Limit { get; set; }
        [QueryString(Name = "radius")]
        public int Radius { get; set; }
        [QueryString(Name = "language")]
        public string Language { get; set; }
        [QueryString(Name = "consignmentLevelDetailsFlag")]
        public bool ConsignmentDetails { get; set; }

        public ShippingCollection ServiceGroups { get; set; }
        public ShippingCollection Skus { get; set; }
        public ShippingRequest()
        {
            WarehousePostcode = "CF72 8YT";
            WarehouseCountryCode = "GBR";
            WarehouseCode = "DESP";
            ReturnType = "lsc";
            ReturnFormat = "json";
            OptionType = "HOME";
            Limit = 10;
            Radius = 10000;
            Language = "en";
            ConsignmentDetails = false;
            UnitWeight = "LB";
        }
        public void AddSkus(IEnumerable<string> skus)
        {
            if (Skus == null)
                Skus = new ShippingCollection();
            Skus.AddRange(skus);
        }
    }
    public class ShippingCollection : List<string>
    {
        public ShippingCollection()
        {

        }
        public ShippingCollection(IEnumerable<string> values)
        {
            this.AddRange(values);
        }
        public override string ToString()
        {
            if (this.Count == 0) return null;
            return string.Join(",", this);
        }
    }
}