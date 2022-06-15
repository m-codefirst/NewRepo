using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using Vattenfall.Domain.Core.Editor.Enums;
using Vattenfall.Domain.Core.Icons;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    public class LibraryIconsFactory : ISelectionFactory
    {
        private readonly IEnumerable<IVattenfallIcon> _icons;

        public LibraryIconsFactory()
        {
            _icons = ServiceLocator.Current.GetInstance<IEnumerable<IVattenfallIcon>>();
        }

        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            //Arranging Icon - None is on the top of list.
            var noneText = Styles.VattenfallIcon.None.ToString();
            var noneIcon = _icons.Where(x => x.Text() == noneText).Select(icon => new SelectItem
            {
                Text = icon.Text(),
                Value = icon.GetIconCssClass()
            });

            return noneIcon.Concat(_icons.Where(x => x.Text() != noneText).Select(icon => new SelectItem
            {
                Text = icon.Text(),
                Value = icon.GetIconCssClass()
            }));
        }
    }
}