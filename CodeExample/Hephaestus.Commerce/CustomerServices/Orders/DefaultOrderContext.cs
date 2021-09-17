using System;
using EPiServer.Logging;
using Mediachase.Commerce.Orders;

namespace Hephaestus.Commerce.CustomerServices.Orders
{
    public class DefaultOrderContext : IAmOrderContext
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(DefaultOrderContext));

        private const string ErrorMessage = "Order Context does not exist";

        public OrderContext CurrentOrderContext
        {
            get
            {
                if (OrderContext.Current != null)
                {
                    return OrderContext.Current;
                }
                _logger.Error(ErrorMessage);
                return null;
            }
        }
    }
}