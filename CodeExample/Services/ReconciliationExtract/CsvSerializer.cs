using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TRM.Web.Services.ReconciliationExtract
{
    public class CsvSerializer
    {
        public static string Serialize<T>(List<T> items) where T : class
        {
            var sb = new StringBuilder();
            var isNameDone = false;
            if (items == null || !items.Any()) return string.Empty;

            var propList = items.FirstOrDefault()?.GetType().GetProperties().ToList();
            if (propList == null || !propList.Any()) return string.Empty;

            foreach (var item in items)
            {
                var propNames = new List<string>();
                var propValues = new List<string>();

                foreach (var prop in propList)
                {
                    if (!isNameDone) propNames.Add(prop.Name);
                    var val = prop.PropertyType == typeof(string) ? "\"{0}\"" : "{0}";
                    propValues.Add(string.Format(val, prop.GetValue(item, null)));
                }
                string line;
                if (!isNameDone)
                {
                    line = string.Join(",", propNames);
                    sb.AppendLine(line);
                    isNameDone = true;
                }
                line = string.Join(",", propValues);
                sb.Append(line);
                sb.AppendLine("");
            }
            return sb.ToString();
        }
    }
}