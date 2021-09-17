using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TRM.Web.Helpers
{
    public class SerializeToJsonHelper
    {
        public static string SerializeToJson(object @object)
        {
            return JsonConvert.SerializeObject(@object, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }
        public static string SerializeToJsonSingleQuote(object @object)
        {
            StringBuilder sb = new StringBuilder();

            using (StringWriter sw = new StringWriter(sb))
            {
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    writer.QuoteChar = '\'';

                    var settings = new JsonSerializerSettings
                    {
                        StringEscapeHandling = StringEscapeHandling.EscapeHtml,
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = NullValueHandling.Include
                    };

                    var ser = JsonSerializer.Create(settings);

                    ser.Serialize(writer, @object);
                }
            }

            return sb.ToString();
        }
    }
}