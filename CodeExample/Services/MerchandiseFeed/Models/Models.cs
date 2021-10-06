using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TRM.Web.Services.MerchandiseFeed.Models
{
    public class NamespacedXmlMediaTypeFormatter : XmlMediaTypeFormatter
    {
        public XmlSerializerNamespaces Namespaces { get; private set; }
        Dictionary<Type, XmlSerializer> Serializers { get; set; }

        public NamespacedXmlMediaTypeFormatter()
        {
            Namespaces = new XmlSerializerNamespaces();
            Namespaces.Add("g", "http://base.google.com/ns/1.0");

            Serializers = new Dictionary<Type, XmlSerializer>();
        }

        public override Task WriteToStreamAsync(
            Type type, object value,
            Stream writeStream, HttpContent content,
            TransportContext transportContext) {
            
            lock (Serializers)
            {
                if (!Serializers.ContainsKey(type))
                {
                    // instantiate the new serializer by passing in the
                    // main XML namespace, in this case the Atom namespace
                    var serializer = new XmlSerializer(type,
                        "http://www.w3.org/2005/Atom");

                    // add a new serializer for this type
                    Serializers.Add(type, serializer);
                }
            }

            return Task.Factory.StartNew(() =>
            {
                XmlSerializer serializer;
                lock (Serializers)
                {
                    serializer = Serializers[type];
                }

                var writerSettings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = false
                };

                var xmlWriter = XmlWriter.Create(writeStream, writerSettings);
                serializer.Serialize(xmlWriter, value, Namespaces);
            });
        }
    }

    [XmlRoot("rss")]
    [Serializable]
    public class Feed
    {
        private string _version = "2.0";

        [XmlAttribute]
        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        [XmlElement("channel")]
        public Channel Channel { get; set; }
    }

    [XmlRoot("channel")]
    [Serializable]
    public class Channel
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("link")]
        public string Link { get; set; }

        [XmlElement("updated")]
        public DateTime Updated { get; set; }

        [XmlElement("item")]
        public List<Entry> Entries { get; set; }
    }

    [XmlType(TypeName = "item")]
    [Serializable]
    public class Entry
    {
        [XmlElement("id", Namespace = "http://base.google.com/ns/1.0")]
        public string Id { get; set; }

        [XmlElement("title", Namespace = "http://base.google.com/ns/1.0")]
        public string Title { get; set; }

        [XmlElement("description", Namespace = "http://base.google.com/ns/1.0")]
        public string Description { get; set; }

        [XmlElement("link", Namespace = "http://base.google.com/ns/1.0")]
        public string Link { get; set; }

        [XmlElement("image_link", Namespace = "http://base.google.com/ns/1.0")]
        public string ImageLink { get; set; }

        [XmlElement("additional_image_link", Namespace = "http://base.google.com/ns/1.0")]
        public string[] AdditionalImageLinks { get; set; }

        [XmlElement("condition", Namespace = "http://base.google.com/ns/1.0")]
        public string Condition { get; set; }

        [XmlElement("availability", Namespace = "http://base.google.com/ns/1.0")]
        public string Availability { get; set; }

        [XmlElement("price", Namespace = "http://base.google.com/ns/1.0")]
        public string Price { get; set; }

        [XmlElement("sale_price", Namespace = "http://base.google.com/ns/1.0")]
        public string SalePrice { get; set; }

        // Changed to support the expected date range, https://support.google.com/merchants/answer/6324460
        [XmlElement("sale_price_effective_date", Namespace = "http://base.google.com/ns/1.0")]
        public string SalePriceEffectiveDate { get; set; }

        [XmlElement("brand", Namespace = "http://base.google.com/ns/1.0")]
        public string Brand { get; set; }

        // Required if item is apparel
        [XmlElement("gender", Namespace = "http://base.google.com/ns/1.0")]
        public string Gender { get; set; }

        // Required if item is apparel
        [XmlElement("age_group", Namespace = "http://base.google.com/ns/1.0")]
        public string AgeGroup { get; set; }

        // Required if item is apparel
        [XmlElement("color", Namespace = "http://base.google.com/ns/1.0")]
        public string Color { get; set; }

        // Required if item is apparel
        [XmlElement("size", Namespace = "http://base.google.com/ns/1.0")]
        public string Size { get; set; }

        [XmlElement("mpn", Namespace = "http://base.google.com/ns/1.0")]
        public string MPN { get; set; }

        [XmlElement("gtin", Namespace = "http://base.google.com/ns/1.0")]
        public string GTIN { get; set; }

        [XmlElement("item_group_id", Namespace = "http://base.google.com/ns/1.0")]
        public string ItemGroupId { get; set; }

        [XmlElement("shipping", Namespace = "http://base.google.com/ns/1.0")]
        public List<Shipping> Shipping { get; set; }

        [XmlElement("google_product_category", Namespace = "http://base.google.com/ns/1.0")]
        public string GoogleProductCategory { get; set; }

        [XmlElement("product_type", Namespace = "http://base.google.com/ns/1.0")]
        public string ProductType { get; set; }

        [XmlElement("shipping_weight", Namespace = "http://base.google.com/ns/1.0")]
        public string ShippingWeight { get; set; }

        [XmlElement("shipping_length", Namespace = "http://base.google.com/ns/1.0")]
        public string ShippingLength { get; set; }

        [XmlElement("shipping_height", Namespace = "http://base.google.com/ns/1.0")]
        public string ShippingHeight { get; set; }

        [XmlElement("shipping_width", Namespace = "http://base.google.com/ns/1.0")]
        public string ShippingWidth { get; set; }

        [XmlElement("identifier_exists", Namespace = "http://base.google.com/ns/1.0")]
        public string IdentifierExists { get; set; }
    }

    [XmlType(TypeName = "shipping")]
    [Serializable]
    public class Shipping
    {
        [XmlElement("country", Namespace = "http://base.google.com/ns/1.0")]
        public string Country { get; set; }

        [XmlElement("service", Namespace = "http://base.google.com/ns/1.0")]
        public string Service { get; set; }

        [XmlElement("price", Namespace = "http://base.google.com/ns/1.0")]
        public string Price { get; set; }
    }
}