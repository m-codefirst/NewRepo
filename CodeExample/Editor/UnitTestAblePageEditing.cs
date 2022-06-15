using System.Diagnostics.CodeAnalysis;
using EPiServer.Editor;
using EPiServer.ServiceLocation;

namespace Vattenfall.Domain.Core.Editor
{
    [ExcludeFromCodeCoverage]
    [ServiceConfiguration(typeof(IPageEditing))]
    public class UnitTestAblePageEditing : IPageEditing
    {
        public bool PageIsInEditMode => PageEditing.PageIsInEditMode;
    }
}
