using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EPiServer.Shell.ObjectEditing;

namespace Vattenfall.Domain.Core.Editor.ContentSelector
{
    [ExcludeFromCodeCoverage]
    public class SitemapPrioritySelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var priorities = new List<SelectItem>
            {
                new SelectItem() {Value = "Default", Text = "Default"},
                new SelectItem() {Value = "1.0", Text = "1.0"},
                new SelectItem() {Value = "0.9", Text = "0.9"},
                new SelectItem() {Value = "0.8", Text = "0.8"},
                new SelectItem() {Value = "0.7", Text = "0.7"},
                new SelectItem() {Value = "0.6", Text = "0.6"},
                new SelectItem() {Value = "0.5", Text = "0.5"}
            };

            return priorities;
        }
    }
}