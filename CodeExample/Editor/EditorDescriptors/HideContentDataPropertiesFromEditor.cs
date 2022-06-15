using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [ExcludeFromCodeCoverage]
    [EditorDescriptorRegistration(TargetType = typeof(EPiServer.Core.ContentData))]
    public class HideContentDataPropertiesFromEditor : EditorDescriptor
    {
        private const string pageExternalURLPropertyName = "PageExternalURL";

        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            base.ModifyMetadata(metadata, attributes);

            foreach (ExtendedMetadata property in metadata.Properties)
            {
                if (property.PropertyName == pageExternalURLPropertyName)
                {
                    property.ShowForEdit = false;
                }
            }
        }
    }
}
