using EPiServer.Shell.ObjectEditing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vattenfall.Domain.Core.Editor.IconSelector;

namespace Vattenfall.Domain.Core.Editor
{
    public class SelectOneIcon : SelectOneAttribute
    {
        public IconEnums.IconCategory IconCategory { get; set; }
    }
}
