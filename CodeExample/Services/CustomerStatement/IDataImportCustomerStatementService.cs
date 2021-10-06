using Mediachase.Commerce.Customers;
using TRM.Web.Models.DTOs.Bullion;

namespace TRM.Web.Services.CustomerStatement
{
    public interface IDataImportCustomerStatementService
    {
        bool InsertConsumerStatementHeader(AxImportData.ConsumerStatementHeader consumerStatement, CustomerContact customer);
        bool InsertConsumerStatementLines(AxImportData.ConsumerStatementHeader consumerStatement, CustomerContact customer);
    }
}