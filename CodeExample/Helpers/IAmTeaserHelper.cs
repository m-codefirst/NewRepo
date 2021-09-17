using EPiServer.Core;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Helpers
{
    public interface IAmTeaserHelper
    {
        TeaserDto GetTeaserDto(IContent currentContent);
        string GetTeaserImageUrl(IContent currentContent);
    }
}