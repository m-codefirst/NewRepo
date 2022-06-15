using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using System;
using System.Collections.Generic;
using System.Linq;
using Vattenfall.Domain.Web.Blocks;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(TitleWithHeadingTypeH2OrH3), EditorDescriptorBehavior = EditorDescriptorBehavior.OverrideDefault, UIHint = UIHint)]
    public class ChangePropertyDescriptionAndDisplayName : EditorDescriptor
    {
        public const string UIHint = "ChangePropertyDescriptionAndDisplayName";
        private const string PropertyName = "HeadingH2H3";
        private const string PropertyDisplayName = "Alternative SEO heading";
        private const string PropertyDescription = "Does not change visual heading (only for Search Engines)";

        public ChangePropertyDescriptionAndDisplayName() : base() { }
        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            base.ModifyMetadata(metadata, attributes);
            foreach (var property in metadata.Properties)
            {
                if (property.PropertyName != PropertyName) continue;

                var propMetadata = property as ExtendedMetadata;
                if (propMetadata?.EditorConfiguration == null || !propMetadata.EditorConfiguration.Any()) continue;
                propMetadata.EditorConfiguration["tooltip"] = PropertyDescription; // Key which change description
                property.Description = PropertyDescription;
                property.DisplayName = PropertyDisplayName;
                property.ShortDisplayName = PropertyDisplayName;
            }
        }
    }
}