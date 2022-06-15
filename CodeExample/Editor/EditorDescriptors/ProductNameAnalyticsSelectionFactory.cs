using EPiServer.Shell.ObjectEditing;
using System.Collections.Generic;


namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    public class ProductNameAnalyticsSelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            return new List<SelectItem>()
            {
                new SelectItem() {Value = "stroom&gas", Text = "stroom&gas"},
                new SelectItem() {Value = "stroom", Text = "stroom"},
                new SelectItem() {Value = "gas", Text = "gas"},
                new SelectItem() {Value = "zonnepanelen", Text = "zonnepanelen"},
                new SelectItem() {Value = "cv-ketel", Text = "cv-ketel"},
                new SelectItem() {Value = "stadswarmte", Text = "stadswarmte"},
                new SelectItem() {Value = "laadpalen", Text = "laadpalen"},
                new SelectItem() {Value = "isolatie", Text = "isolatie"},
                new SelectItem() {Value = "ventilatie", Text = "ventilatie"},
                new SelectItem() {Value = "huisbeveiliging", Text = "huisbeveiliging"},
                new SelectItem() {Value = "leegstandscontract", Text = "leegstandscontract"},
                new SelectItem() {Value = "flexibele-prijs", Text = "flexibele-prijs"},
                new SelectItem() {Value = "vasteprijs", Text = "vasteprijs"},
                new SelectItem() {Value = "herprijs", Text = "herprijs"},
                new SelectItem(){Value = "split-online", Text = "split-online"},
                new SelectItem(){Value = "gespreid-inkopen", Text = "gespreid-inkopen"},
                new SelectItem(){Value = "nederlandse-wind", Text = "nederlandse-wind"},
                new SelectItem(){Value = "europese-wind", Text = "europese-wind"},
                new SelectItem(){Value = "stadkoude", Text = "stadkoude"}
            };
        }
    }
}