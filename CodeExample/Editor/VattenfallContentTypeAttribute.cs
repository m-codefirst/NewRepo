using EPiServer;
using EPiServer.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor
{
    /// <summary>
    /// Attribute used for site content types to set default attribute values, Default group name is default
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class VattenfallContentTypeAttribute : ContentTypeAttribute
    {
        public VattenfallContentTypeAttribute()
        {
            GroupName = GroupNames.Vattenfall;            
        }
    }
}
