using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce.Customers;
using Newtonsoft.Json;

namespace TRM.Web.Extentions
{
    public static class CustomerContactExtensions
    {
        public static T DeserializeProperty<T>(this CustomerContact contact, string propertyName)
        {
            var serializeProperty = contact.Properties[propertyName]?.Value?.ToString();

            return string.IsNullOrWhiteSpace(serializeProperty) ? default(T) : JsonConvert.DeserializeObject<T>(serializeProperty);
        }

        public static void SerializePropertyAndSave<T>(this CustomerContact contact, string propertyName, T value)
        {
            if (contact == null) return;

            var serializeProperty = JsonConvert.SerializeObject(value);
            var prop = contact.Properties[propertyName];
            if (prop != null)
            {
                prop.Value = serializeProperty;
            }
            else
            {
                contact.Properties.Add(new EntityObjectProperty(propertyName, serializeProperty));
            }

            contact.SaveChanges();
        }
    }
}