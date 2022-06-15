using EPiServer.Core;
using EPiServer.PlugIn;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [PropertyDefinitionTypePlugIn]
    public class InternalKeywordListTypeDefinition : PropertyList<InternalKeyword>
    {
    }
}
