using EPiServer.Core;
using TRM.Web.Models.Find;

namespace TRM.Web.Helpers.Find
{
    public interface IAddCommerceSearchFacets : IContentData
    {
        string Term { get; set; }
        string Name { get; set; }

        bool ShowExpanded { get; set; }

        FacetType FacetType { get; }
        int StartPoint { get; set; }
        int Increment { get; set; }
        int EndPoint { get; set; }
    }
}
