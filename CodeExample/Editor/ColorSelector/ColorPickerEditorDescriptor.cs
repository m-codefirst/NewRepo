using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor.ColorSelector
{
    [ExcludeFromCodeCoverage]
    [EditorDescriptorRegistration(TargetType = typeof(string), UIHint = "ColorPickerEditor")]
    public class ColorPickerEditorDescriptor : EditorDescriptor
    {
        public override void ModifyMetadata(EPiServer.Shell.ObjectEditing.ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            SelectionFactoryType = typeof(ColorSelectionFactory);
            ClientEditingClass = "vattenfall/editors/ColorPickerEditor";

            base.ModifyMetadata(metadata, attributes);
        }
    }
}
