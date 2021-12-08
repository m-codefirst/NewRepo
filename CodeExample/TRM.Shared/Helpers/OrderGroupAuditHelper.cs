using System;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;

namespace TRM.Shared.Helpers
{
    public class OrderGroupAuditHelper : IAmOrderGroupAuditHelper
    {
        public void WriteAudit(IOrderGroup orderGroup, string title, string note)
        {
            var on = orderGroup?.CreateOrderNote();
            if (on == null) return;

            on.Detail = $"[{Environment.MachineName}] :: {note}";
            on.Title = $"{title} : [{Environment.MachineName}]";
            on.Type = OrderNoteTypes.Custom.ToString();
            on.Created = DateTime.Now;
            on.CustomerId = orderGroup.CustomerId;
            orderGroup.Notes.Add(on);
        }
    }
}
