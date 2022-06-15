namespace Vattenfall.Domain.Core.Editor.EditorDescriptors
{
    /// <summary>
    /// Class to store the RowHEader and the Description in Episerver
    /// </summary>
    public class DataTableEnhancedRowInfo
    {
        /// <summary>
        /// Identifier
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Row header in the DataTableEnhanced
        /// </summary>
        public string RowHeader { get; set; }
        /// <summary>
        /// Description of the info that is identified by the key
        /// </summary>
        public string Description { get; set; }
    }
}
