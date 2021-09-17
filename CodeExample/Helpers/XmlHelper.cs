using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TRM.Web.Helpers
{
    public class XmlHelper : IAmXmlHelper
    {
        public XmlDocument Serialize<T>(T source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Object to serialize cannot be null");

            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t"
            };

            XmlDocument doc = new XmlDocument();

            MemoryStream memoryStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings);

            XmlSerializer x = new XmlSerializer(typeof(T));
            x.Serialize(xmlWriter, source);

            memoryStream.Position = 0; // rewind the stream before reading back.
            using (StreamReader sr = new StreamReader(memoryStream))
            {
                doc.LoadXml(sr.ReadToEnd());
            }
            return doc;
        }
    }
}