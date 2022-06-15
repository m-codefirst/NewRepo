using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor.ColorSelector
{
    [ExcludeFromCodeCoverage]
    [EditorDescriptorRegistration(TargetType = typeof(string), UIHint = UIHint)]
    public class IconPickerEditorDescriptor : EditorDescriptor
    {
        public const string UIHint = "IconPickerEditor";
        public override void ModifyMetadata(EPiServer.Shell.ObjectEditing.ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            ClientEditingClass = "vattenfall/editors/IconPickerEditor";

            base.ModifyMetadata(metadata, attributes);
        }
    }
}
