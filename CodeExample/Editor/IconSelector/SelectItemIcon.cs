using EPiServer.Shell.ObjectEditing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vattenfall.Domain.Core.Icons;

namespace Vattenfall.Domain.Core.Editor.IconSelector
{
    public class SelectItemIcon : SelectItem
    {
        private IVattenfallIcon icon;
        public SelectItemIcon(IVattenfallIcon icon)
        {
            this.icon = icon;
        }

        public new object Value
        {
            get
            {
                return icon.GetIconCssClass();
            }
        }
        public new string Text
        {
            get
            {
                return icon.Text();
            }
        }
    }
}
