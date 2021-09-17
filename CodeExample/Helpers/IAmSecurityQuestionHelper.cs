using System.Collections.Generic;

namespace TRM.Web.Helpers
{
    public interface IAmSecurityQuestionHelper
    {
        Dictionary<string, string> GetSecurityQuestionList(string message = "");
        string GetQuestionById(string id);
        int GetTwoPartQuestionId(string question);
    }
}