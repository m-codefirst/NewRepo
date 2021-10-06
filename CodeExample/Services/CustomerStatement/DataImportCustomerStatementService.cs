using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.Web.Business.DataAccess;
using TRM.Web.Extentions;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.EntityFramework.Statements;

namespace TRM.Web.Services.CustomerStatement
{

    [ServiceConfiguration(typeof(IDataImportCustomerStatementService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DataImportCustomerStatementService : IDataImportCustomerStatementService
    {
        private readonly ICustomerStatementRespository _statementRespository;
        public DataImportCustomerStatementService(ICustomerStatementRespository statementRespository)
        {
            _statementRespository = statementRespository;
        }
        
        public bool InsertConsumerStatementHeader(AxImportData.ConsumerStatementHeader axStatement, CustomerContact customer)
        {
            var statementHeader = AxToDbStatementHeaderMapper(axStatement);
            return _statementRespository.InsertCustomerStatementHeader(statementHeader);
        }

        public bool InsertConsumerStatementLines(AxImportData.ConsumerStatementHeader axStatement, CustomerContact customer)
        {
            if (!axStatement.ConsumerStatementLines.Any()) return false;

            var statementLines = AxToDbStatementLinesMapper(axStatement);
            return _statementRespository.InsertCustomerStatementLines(statementLines);
        }

        private IEnumerable<StatementLine> AxToDbStatementLinesMapper(AxImportData.ConsumerStatementHeader axStatement)
        {
            return axStatement.ConsumerStatementLines.Select(x => new StatementLine
            {
                StatementLineId = Guid.NewGuid(),
                CustomerRef = axStatement.AccountNum,
                TransDate = x.TransDate.ToSqlDatetime(),
                Reference = x.Reference,
                StatementNum = x.StatementNum,
                LineAmount = x.LineAmount,
                InventorySubTotal = x.SubTotInv,
                PaymentsSubTotal = x.SubTotPayments,
                DebitsSubTotal = x.SubTotDebits,
                CreditsSubTotal = x.SubTotCredits,
                PostageSubTotal = x.SubTotPostage,
                ProductName = x.ProductName,
                Qty = x.Qty
            });
        }

        private static Statement AxToDbStatementHeaderMapper(AxImportData.ConsumerStatementHeader axStatement)
        {
            return new Statement
            {
                StatementId = Guid.NewGuid(),
                StatementNum = axStatement.StatementNum,
                CustomerRef = axStatement.AccountNum,
                LocationName = axStatement.LocationName,
                Street = axStatement.Street,
                City = axStatement.City,
                State = axStatement.City,
                County = axStatement.County,
                PostCode = axStatement.ZipCode,
                CountryCode = axStatement.CountryRegionId,
                StatementIssueDate = axStatement.StatementIssueDate.ToSqlDatetime(),
                BalanceBroughtForward = axStatement.BalanceBroughtForward,
                NewBalance = axStatement.NewBalance,
                PaymentDueDate = axStatement.PaymentDueDate.ToSqlDatetime(),
                MinimumPayment = axStatement.MinimumPaymentCustCur,
                CreditMax = axStatement.CreditMax,
                Note = axStatement.Note
            };
        }
    }
}