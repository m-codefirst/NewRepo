using System.Collections.Generic;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Services
{
    public interface IEmailService
    {
        bool SendEmail(List<MailAddress> to, MailAddress from, List<MailAddress> cc, List<MailAddress> bcc, string subject, string emailBody, bool isHtml = true, Dictionary<string, string> settings = null);
    }
}