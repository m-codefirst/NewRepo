using System.ComponentModel.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using Vattenfall.Domain.Core.Editor.Enums;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    public class InternalKeyword
    {
        [BackingType(typeof(PropertyNumber))]
        [EditorDescriptor(EditorDescriptorType = typeof(EnumEditorDescriptor<Styles.KeywordType>))]
        [Display(Name = "Type")]
        public Styles.KeywordType KeywordType { get; set; }

        [Display(Name = "Keyword(s)")]
        public string Keywords { get; set; }

        [Display(Name = "Volume / Anders")]
        public string Volume { get; set; }
    }
}
