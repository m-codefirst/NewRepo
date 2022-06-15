using EPiServer.Shell.ObjectEditing;
using System.Collections.Generic;


namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    public class ProductCategoryAnalyticsSelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            return new List<SelectItem>()
            {
                new SelectItem(){Value = "energie", Text = "energie"},
                new SelectItem(){Value = "zonnepanelen", Text = "zonnepanelen"},
                new SelectItem(){Value = "stadswarmte", Text = "stadswarmte"},
                new SelectItem(){Value = "overig", Text = "overig"}
            };
        }
    }
}