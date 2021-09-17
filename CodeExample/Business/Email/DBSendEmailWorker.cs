using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using EPiServer.ServiceLocation;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs;
using System.IO;
using TRM.IntegrationServices.Models.Export.Emails;
using static TRM.Shared.Constants.Enums;
using TRM.IntegrationServices.Interfaces;

namespace TRM.Web.Business.Email
{
    public class DbSendEmailWorker : ISendEmailWorker
    {     
        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public Dictionary<string, string> Settings { get; set; }

        /// <summary>
        /// Gets the type of the transmission.
        /// </summary>
        /// <value>
        /// The type of the transmission.
        /// </value>
        public Enums.EmailTransmissionType TransmissionType
        {
            get { return Enums.EmailTransmissionType.CustomDB; }
        }
        
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Init()
        {
            if (Settings == null || !Settings.Any())
                throw new ArgumentException("No DB settings have been provided.");
        
            if (!Settings.ContainsKey("Label"))
                throw new ArgumentException("message label setting has not been provided.");

        
        }

        /// <summary>
        /// Sends this email through MSMQ
        /// </summary>
        /// <param name="emailRequest">The email request.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">email request object is not set
        /// or
        /// No MSMQ settings have been provided.</exception>
        public bool Send(SendEmailRequest emailRequest)
        {
            var xmlDocument = new XmlDocument();
            try
            {
                if (emailRequest == null) throw new ArgumentException("email request object is not set");

                var nav = xmlDocument.CreateNavigator();
                using (var writer = nav.AppendChild())
                {
                    var ns = new XmlSerializerNamespaces();
                    ns.Add("maginus", "icore.TrmIntegration.schemas.adapters.SendEmailRequest");
                    var ser = new XmlSerializer(emailRequest.GetType());
                    ser.Serialize(writer, emailRequest, ns);
                }

                var xml = string.Empty;
                using (Stream stream = new MemoryStream())
                {                    
                    xmlDocument.Save(stream);
                    stream.Position = 0;
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        xml = reader.ReadToEnd();
                    }
                }
                var msg = new EmailMessage
                {
                   EmailMessageId = Guid.NewGuid(),
                   MessageType = Settings["Label"],
                   EmailBody = xml,
                   ExportStatus = eMailExportStatus.Pending.ToString(),
                   Created = DateTime.Now
                };
                var emailMessageRepository = ServiceLocator.Current.GetInstance<IEmailMessageRepository>();
                emailMessageRepository.AddEmailMessage(msg);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}