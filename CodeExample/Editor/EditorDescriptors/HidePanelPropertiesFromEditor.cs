using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Vattenfall.Domain.Web.Pages;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [ExcludeFromCodeCoverage]
    [EditorDescriptorRegistration(TargetType = typeof(WebBasePage))]
    public class HidePanelPropertiesFromEditor : EditorDescriptor
    {
        private const string IsHalfPanelBackgroundColorPropertyName = "IsHalfPanelBackgroundColor";
        private const string HalfPanelCssSelectorPropertyName = "HalfPanelCssSelector";

        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            base.ModifyMetadata(metadata, attributes);

            if (metadata.PropertyName == IsHalfPanelBackgroundColorPropertyName && !string.IsNullOrWhiteSpace(metadata.SimpleDisplayText) && metadata.SimpleDisplayText == "True")
                HideProperty(metadata, IsHalfPanelBackgroundColorPropertyName);
            if (metadata.PropertyName == HalfPanelCssSelectorPropertyName && !string.IsNullOrWhiteSpace(metadata.SimpleDisplayText))
                HideProperty(metadata, HalfPanelCssSelectorPropertyName);
        }

        /// <summary>
        /// Hide property to display on Editor page
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="propertyName"></param>
        private void HideProperty(ExtendedMetadata metadata, string propertyName)
        {
            if (metadata.PropertyName == propertyName)
            { 
                metadata.ShowForEdit = false;
                metadata.HideSurroundingHtml = true;
                metadata.ShowForDisplay = false;
            }
        }
    }
}
