using System;

namespace TRM.Shared.Interfaces
{
    public interface IControlAtp
    {
        DateTime AtpDate { get; set; }
        int LeadTimeDays { get; set; }
    }
}