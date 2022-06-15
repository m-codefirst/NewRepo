using EPiServer.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor
{
    [ExcludeFromCodeCoverage]
    public class VattenfallDefaultBlockThumbnail : ImageUrlAttribute
    {
        /// <summary>
        /// The parameterless constructor will initialize a VattenfallPageIcon attribute with a default thumbnail
        /// </summary>
        public VattenfallDefaultBlockThumbnail() : base("~/Static/img/episerver/components/content-block.png")
        {

        }

        public VattenfallDefaultBlockThumbnail(string path) : base(path)
        {

        }
    }
}