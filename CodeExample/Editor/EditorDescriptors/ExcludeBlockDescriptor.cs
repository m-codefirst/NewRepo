using EPiServer.Shell;
using System.Diagnostics.CodeAnalysis;
using Vattenfall.Domain.Core.Blocks;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [ExcludeFromCodeCoverage]
    [UIDescriptorRegistration]
    public class ExcludeBlockDescriptor : UIDescriptor<IExcludeBlock> { }
}
