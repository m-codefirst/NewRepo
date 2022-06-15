using EPiServer.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor
{
    [ExcludeFromCodeCoverage]
    public class VattenfallDefaultPageThumbnail : ImageUrlAttribute
    {
        /// <summary>
        /// The parameterless constructor will initialize a VattenfallPageIcon attribute with a default thumbnail
        /// </summary>
        public VattenfallDefaultPageThumbnail() : base("~/Static/img/episerver/pages/home-page.png")
        {

        }

        public VattenfallDefaultPageThumbnail(string path) : base(path)
        {

        }
    }
}