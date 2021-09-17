using System.Collections.Generic;
using TRM.Web.Models.EntityFramework.Statements;

namespace TRM.Web.Business.DataAccess
{
    public interface ICustomerStatementRespository
    {
        bool InsertCustomerStatementHeader(Statement statement);
        bool InsertCustomerStatementLines(IEnumerable<StatementLine> statementLine);

    }
}