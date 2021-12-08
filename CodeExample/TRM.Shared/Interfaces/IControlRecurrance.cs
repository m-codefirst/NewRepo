using TRM.Shared.Constants;

namespace TRM.Shared.Interfaces
{
    public interface IControlRecurrence
    {
        Enums.eRecurrenceType RecurrenceType { get; set; }
        string RecurringFirstVariantSkuCode { get; set; }
    }
}