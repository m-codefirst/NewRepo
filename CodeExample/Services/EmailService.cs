using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using TRM.Web.Business.Email;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs;
using MailAddress = TRM.Web.Models.DTOs.MailAddress;

namespace TRM.Web.Services
{
    public class EmailService : IEmailService
    {
        public Enums.EmailTransmissionType TransmissionType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailService"/> class.
        /// </summary>
        /// <param name="transmissionType">Type of the transmission.</param>
        public EmailService(Enums.EmailTransmissionType transmissionType)
        {
            TransmissionType = transmissionType;
        }

        /// <summary>
        /// Sends the email.  This is posted to a MSMQ
        /// </summary>
        /// <param name="to">To.</param>
        /// <param name="from">From.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="bcc">The BCC.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="emailBody">The email body.</param>
        /// <param name="isHtml">if set to <c>true</c> [is HTML].</param>
        /// <param name="settings">The settings.</param>
        /// <returns>
        /// result of wether the sending of the email worked.
        /// </returns>
        /// <exception cref="System.ApplicationException">Email worker was not instantiated</exception>
        public bool SendEmail(List<MailAddress> to, MailAddress from, List<MailAddress> cc, List<MailAddress> bcc, string subject, string emailBody, bool isHtml = true, Dictionary<string, string> settings = null)
        {
            var logger = LogManager.GetLogger(typeof(EmailService));

            // init
            if (settings == null) settings = new Dictionary<string, string>();

            try
            {
                logger.Info("Start sending email");

                // Build email request object
                var emailRequest = new SendEmailRequest
                {
                    Email = new Email
                    {
                        Body = emailBody,
                        From = new MailAddress
                        {
                            Address = from.Address,
                            DisplayName = from.DisplayName
                        },
                        To = to,
                        Bcc = bcc,
                        Cc = cc,
                        Subject = subject,
                        IsHtml = isHtml
                    }
                };

                ISendEmailWorker emailWorker = null;

                if (TransmissionType == Enums.EmailTransmissionType.CustomDB)
                {
                    emailWorker = new DbSendEmailWorker
                    {
                        Settings = settings
                    };

                }
                // Throw if email worker not set 
                if (emailWorker == null) throw new ApplicationException("Email worker was not instantiated");

                logger.Info(string.Format("Start sending email to address {0}", emailRequest.Email.To.First().Address));

                // Init to set up the queue details
                emailWorker.Init();

                // Send
                emailWorker.Send(emailRequest);

                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Email failed to send", ex);
                throw;
            }
        }
    }
}