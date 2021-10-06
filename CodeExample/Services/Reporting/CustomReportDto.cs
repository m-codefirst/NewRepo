using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TRM.Web.Services.Reporting
{
    public class CustomReportDto<T>
    {
        public CustomReportDto()
        {
            this.Headings = CustomReportHelper.GetHeadings<T>();
        }


        public IEnumerable<T> Rows { get; set; }

        public string Name { get; set; }

        public IEnumerable<HeadingDto> Headings { get; }

        public int TotalMatching { get; set; }

        public int TotalPages { get; set; }

        public int CurrentPage { get; set; }

        public int RowsPerPage { get; set; }
    }

    public class HeadingDto
    {
        public string DisplayName { get; set; }

        public string PropName { get; set; }
    }

    public class CustomReportHelper
    {
        public static IEnumerable<HeadingDto> GetHeadings<T>()
        {
            var headings = typeof(T).GetProperties()
                .Select(x => new {Attribute = x.GetCustomAttribute(typeof(DisplayNameAttribute), true), PropName = x.Name})
                .Select(x => new HeadingDto
                    {DisplayName = (x.Attribute as DisplayNameAttribute)?.DisplayName ?? x.PropName, PropName = x.PropName});

            return headings;
        }

        public static List<object> GetValues(object row)
        {
            var values = row.GetType().GetProperties().Select(prop => prop.GetValue(row)).ToList();

            return values;
        }
    }
}