using System.Collections.Generic;
using System.Linq;
using TRM.Web.Models.EntityFramework.Statements;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public class StatementHelper : IAmStatementHelper
    {
        public List<MyAccountStatementHistoryHeading> GetAccountStatementHistoryHeadings(string customerRef)
        {
            using (var db = new StatementsContext(Shared.Constants.StringConstants.TrmCustomDatabaseName))
            {
                return db.Statements.Where(a => a.CustomerRef.ToLower() == customerRef.ToLower())
                    .OrderByDescending(a => a.StatementIssueDate).Select(a => new MyAccountStatementHistoryHeading
                    {
                        Id = a.StatementNum.ToString(),
                        StatementDate = a.StatementIssueDate
                    }).ToList();
            }
        }

        public MyAccountStatementItem GetAccountStatementDetails(string id, string customerCode)
        {
            using (var db = new StatementsContext(Shared.Constants.StringConstants.TrmCustomDatabaseName))
            {
                var statement = db.Statements.FirstOrDefault(a => a.StatementNum == id
                                                                && a.CustomerRef == customerCode);

                if (statement != null)
                {
                    var myAccountStatement = new MyAccountStatementItem
                    {
                        StatementDate = statement.StatementIssueDate,
                        PaymentDueDate = statement.PaymentDueDate,
                        CustomerRef = statement.CustomerRef,
                        CreditLimit = statement.CreditMax,
                        BalanceBroughtForward = statement.BalanceBroughtForward,
                        NewBalance = statement.NewBalance,
                        MinPayment = statement.MinimumPayment,
                        LocationName = statement.LocationName,
                        Street = statement.Street,
                        City = statement.City,
                        County = statement.County,
                        PostCode = statement.PostCode,
                        Note = statement.Note
                    };

                    var statementLines = db.StatementLines.Where(a => a.StatementNum.ToString() == id && a.CustomerRef == customerCode);

                    if (statementLines.Any())
                    {
                        myAccountStatement.Items = statementLines.Select(a => new MyAccountStatementLineItem
                        {
                            TransactionDate = a.TransDate,
                            InvoiceNumber = a.Reference,
                            Quantity = a.Qty,
                            Description = a.ProductName,
                            Amount = a.InventorySubTotal,
                            Postage = a.PostageSubTotal,
                            Credits = a.CreditsSubTotal
                        }).ToList();
                    }

                    return myAccountStatement;
                }
            }

            return new MyAccountStatementItem();
        }
    }
}