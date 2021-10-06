using System.Collections.Generic;
using TRM.IntegrationServices.Models.Import;

namespace TRM.Web.Services.Import
{
    public interface IAxDataImportService
    {
        ImportXmlResponse ImportDataFromAx(ImportXmlRequest importXmlRequest);
    }
}