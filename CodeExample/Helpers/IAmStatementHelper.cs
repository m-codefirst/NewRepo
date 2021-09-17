using System.Collections.Generic;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public interface IAmStatementHelper
    {
        List<MyAccountStatementHistoryHeading> GetAccountStatementHistoryHeadings(string customerRef);
        MyAccountStatementItem GetAccountStatementDetails(string id, string customerCode);
    }
}