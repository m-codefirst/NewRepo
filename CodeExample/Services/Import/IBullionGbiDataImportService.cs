using TRM.IntegrationServices.Models;
using TRM.IntegrationServices.Models.Import;

namespace TRM.Web.Services.Import
{
    public interface IBullionGbiDataImportService
    {
        TrmResponse ImportBullionGbiData(ImportXmlRequest importXmlRequest);
    }
}