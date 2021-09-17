using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace TRM.Web.Helpers
{
    public static class FormCollectionDataBinder
    {
        public static T Bind<T>(NameValueCollection collection) where T : new()
        {
            var result = new T();

            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                var key = propertyInfo.Name;
                if (propertyInfo.CanRead && collection.AllKeys.Contains(key))
                {
                    try
                    {
                        var value = collection[key];
                        propertyInfo.SetValue(result, value);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return result;
        }
    }
}