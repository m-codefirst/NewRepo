using System.Collections.Generic;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public interface IBullionVatStatusesHelper
    {
        List<BullionVatStatuses> GetBullionVatStatuses();
    }
}