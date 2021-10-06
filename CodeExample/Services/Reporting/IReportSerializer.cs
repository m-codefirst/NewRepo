using System.Collections.Generic;
using System.Linq;
using System.Text;
using TRM.Web.Services.ReconciliationExtract;

namespace TRM.Web.Services.Reporting
{
    public interface IReportSerializer
    {
        byte[] GetBytes(IEnumerable<object> data);
    }

    public class ReportSerializer : IReportSerializer
    {
        public byte[] GetBytes(IEnumerable<object> data)
        {
            string content = this.GetCsvString(data);

            return Encoding.UTF8.GetBytes(content);
        }

        private string GetCsvString(IEnumerable<object> data)
        {
            return CsvSerializer.Serialize(data.ToList());

        }
    }
}