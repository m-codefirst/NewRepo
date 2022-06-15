using EPiServer.Core;
using EPiServer.PlugIn;
using Newtonsoft.Json;

namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    [PropertyDefinitionTypePlugIn]
    public class DataTableEnhancedRowInfoList : PropertyList<DataTableEnhancedRowInfo>
    {
        protected override DataTableEnhancedRowInfo ParseItem(string value)
        {
            return JsonConvert.DeserializeObject<DataTableEnhancedRowInfo>(value);
        }
    }
}
