using System;

namespace TRM.Web.Services.Metapack.Models.Response
{
    public class ShippingResponse
    {
        public ShippingHeader Header { get; set; }
        public ShippingResult[] Results { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class ShippingHeader
    {
        public DateTime RequestDate { get; set; }
        public Guid RequestId { get; set; }
        public DateTime ResponseDate { get; set; }
    }
    public class ShippingResult
    {
        public string BookingCode { get; set; }
        public string CarrierCode { get; set; }
        public string CarrierServiceCode { get; set; }
        public string CarrierServiceName { get; set; }
        public Collection Collection { get; set; }
        public Delivery Delivery { get; set; }
        public string FullName { get; set; }
        public string[] GroupCodes { get; set; }
        public DateTime CutOffDateTime { get; set; }
        public decimal ShippingCharge { get; set; }
        public double? Lat { get; set; }
        public double? Long { get; set; }
        public StoreTimes StoreTimes { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }
        public Distance Distance { get; set; }
        public string StoreId { get; set; }
        public string StoreName { get; set; }
        public string[] PhotoUrls { get; set; }
        public string LogoUrl { get; set; }
        public bool HasDisabledAccess { get; set; }
        public string TelephoneNumber { get; set; }
        public Guid LocationProviderId { get; set; }
        public string OptionType { get; set; }
    }
    public class Collection
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
    public class Delivery
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
    public class Distance
    {
        public double Value { get; set; }
        public string Unit { get; set; }
    }
    public class StoreTimes
    {
        public string[] Monday { get; set; }
        public string[] Tuesday { get; set; }
        public string[] Wednesday { get; set; }
        public string[] Thursday { get; set; }
        public string[] Friday { get; set; }
        public string[] Saturday { get; set; }
        public string[] Sunday { get; set; }
    }
}