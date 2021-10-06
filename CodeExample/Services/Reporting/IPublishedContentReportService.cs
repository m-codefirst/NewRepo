using System;
using System.ComponentModel;
using EPiServer.Core;

namespace TRM.Web.Services.Reporting
{
    public interface IPublishedContentReportService
    {
        CustomReportDto<PublishedContentReportRow> GetFullReport(DateTime? @from, DateTime? to, string author, string language);

        CustomReportDto<PublishedContentReportRow> GetReport(DateTime? @from, DateTime? to, int page, int itemsPerPage, bool @ascending,
            string sortField, string author, string language);
    }

    public class PublishedContentReportRow
    {
        public string Name { get; set; }

        //Saved date on the last version with status PUBLISHED
        [DisplayName("Last Published")]
        public DateTime? LastPublished { get; set; }      
        
        [DisplayName("Changed By")]
        public string ChangedBy { get; set; }

        [DisplayName("First Published Date")]
        public DateTime? StartPublish { get; set; }

        public string Language { get; set; }

        [DisplayName("Content Type")]
        public string ContentType { get; set; }

        [DisplayName("Content Link")]
        public ContentReference ContentLink { get; set; }
    }
}