using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace TRM.Web.Services.MerchandiseFeed.Pages
{
    [ContentType(
        DisplayName = "FeedPage", 
        GUID = "e2b936ef-30de-47e3-a405-9ace81743eca", 
        Description = "Page for Feed.", 
        GroupName = "Specialized")]
    public class FeedPage : PageData
    {
        [Editable(false)]
        [ScaffoldColumn(false)]
        public virtual string Output { get; set; }
    }
}