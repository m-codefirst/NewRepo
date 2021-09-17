namespace Hephaestus.Commerce.Email
{
    public interface ISendEmail
    {
        bool SendEmail(string fromEmailAddress, string toEmailAddress, string smtpAddress, string userName, string password, bool ssl, int portNo, string message, string subject);
    }
}
