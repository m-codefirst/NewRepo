using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EPiServer.Shell;
using Vattenfall.Domain.Core.Pages;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [ExcludeFromCodeCoverage]
    [UIDescriptorRegistration]
    public class ShowAllPropertiesOnlyToEditorDescriptor : UIDescriptor<IShowAllPropertiesOnlyToEditor>
    {
        public ShowAllPropertiesOnlyToEditorDescriptor()
        {
            DefaultView = CmsViewNames.AllPropertiesView;
            EnableStickyView = false;
            DisabledViews = new List<string> { CmsViewNames.OnPageEditView, CmsViewNames.PreviewView };
        }
    }
}
