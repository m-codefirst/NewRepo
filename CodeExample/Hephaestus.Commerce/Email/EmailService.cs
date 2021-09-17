using System;
using System.Net.Mail;
using EPiServer.Logging.Compatibility;

namespace Hephaestus.Commerce.Email
{
    public class EmailService : ISendEmail
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EmailService));
        public bool SendEmail(string fromEmailAddress, string toEmailAddress, string smtpAddress, string userName,
            string password, bool ssl, int portNo, string message, string subject)
        {
            try
            {
                var emailMsg = new MailMessage(fromEmailAddress, toEmailAddress)
                {
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = message
                };

                var mailClient = new SmtpClient();

                if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                {
                    var basicAuthenticationInfo = new System.Net.NetworkCredential(userName, password);
                    mailClient.UseDefaultCredentials = false;
                    mailClient.Credentials = basicAuthenticationInfo;
                }
                else
                {
                    mailClient.UseDefaultCredentials = true;
                }

                if (ssl)
                {
                    mailClient.EnableSsl = true;
                }

                if (portNo > 0)
                {
                    mailClient.Port = portNo;
                }

                mailClient.Host = smtpAddress;


                mailClient.Send(emailMsg);

                return true;
            }
            catch (Exception ex)
            {

                Logger.Info(string.Format("Error occur sending email to user [{0}]. The exception is:{1}", toEmailAddress,ex));
                return false;
            }


        }
    }
}
