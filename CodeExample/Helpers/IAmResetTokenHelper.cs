
namespace TRM.Web.Helpers
{
    public interface IAmResetTokenHelper
    {
        string CreatePasswordResetToken(string username);
        bool ValidatePasswordResetToken(string username, string token);
        bool DeletePasswordResetToken(string token);
        int DeleteExpiredTokens();
       
    }
}