using System.Collections.Generic;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Business.Email
{
    public interface ISendEmailWorker
    {
        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        Dictionary<string, string> Settings { get; set; }

        /// <summary>
        /// Sends this instance.
        /// </summary>
        /// <param name="emailRequest">The email request.</param>
        /// <returns></returns>
        bool Send(SendEmailRequest emailRequest);

        /// <summary>
        /// Gets the type of the transmission.
        /// </summary>
        /// <value>
        /// The type of the transmission.
        /// </value>
        Enums.EmailTransmissionType TransmissionType { get; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Init();
    }
}