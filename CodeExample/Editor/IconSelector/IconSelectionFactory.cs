using EPiServer.Shell.ObjectEditing;
using System.Collections.Generic;
using System.Linq;

namespace Vattenfall.Domain.Core.Editor.IconSelector
{
    public class IconSelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            SelectOneIcon selectOneIconAttribute = metadata.Attributes.OfType<SelectOneIcon>().FirstOrDefault();
            return Constants.IconSelections.Icons[selectOneIconAttribute.IconCategory].OrderBy(icon => icon.Text).ToList();
        }
    }
}
