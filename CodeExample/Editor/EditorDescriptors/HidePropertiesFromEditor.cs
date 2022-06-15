using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using Vattenfall.Domain.Core.Pages;
using Vattenfall.Domain.Web.Pages;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [ExcludeFromCodeCoverage]
    [EditorDescriptorRegistration(TargetType = typeof(WebBasePage))]
    public class HidePropertiesFromEditor : EditorDescriptor
    {
        private const string ClosestHomePagePropertyName = "ClosestHomePage";
        private const string CustomChangedPropertyName = "CustomChanged";
        private const string ReferralVideoPropertyName = "ReferralVideo";
        private const string AllowFullScreenPropertyName = "AllowFullScreen";
        private const string IsHalfPanelBackgroundColorPropertyName = "IsHalfPanelBackgroundColor";
        private const string HalfPanelCssSelectorPropertyName = "HalfPanelCssSelector";

        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            base.ModifyMetadata(metadata, attributes);

            HideProperty(metadata, ClosestHomePagePropertyName);
            HideProperty(metadata, CustomChangedPropertyName);
            HideProperty(metadata, ReferralVideoPropertyName);
            HideProperty(metadata, AllowFullScreenPropertyName);
            //HideProperty(metadata, IsHalfPanelBackgroundColorPropertyName);
            //HideProperty(metadata, HalfPanelCssSelectorPropertyName);
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
