using System;
using System.Collections.Generic;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using TRM.IntegrationServices.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.Statements;

namespace TRM.Web.Business.DataAccess
{
    [ServiceConfiguration(typeof(ICustomerStatementRespository), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CustomerStatementRespository : DbContextDisposable<StatementsContext>, ICustomerStatementRespository
    {
        private static ILog Logger = LogManager.GetLogger(typeof(CustomerStatementRespository));
        public bool InsertCustomerStatementHeader(Statement statement)
        {
            try
            {
                if(statement == null) throw new Exception("Cannot insert the null statement into the table Statements");
                context.Statements.Add(statement);
                context.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
        }

        public bool InsertCustomerStatementLines(IEnumerable<StatementLine> statementLine)
        {
            try
            {
                if (statementLine == null) throw new Exception("Cannot insert the null statement line into the table StatementLines");
                context.StatementLines.AddRange(statementLine);
                context.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
        }
    }
}