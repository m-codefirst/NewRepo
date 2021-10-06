using System;
using System.Collections.Generic;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Services.InvoiceStatements
{
    public interface IBullionStatementService
    {
        List<DocumentDto> GetBullionDocumentList(Guid customerId, int year);
        List<int> GetBullionDocumentYearList(Guid customerId);

        string GenerateStatementDetailReport(Guid statementId);
        bool AddCustomerStatementFromImportData(AxImportData.CustomerStmtsStatement statement, Guid customerId);
    }
}