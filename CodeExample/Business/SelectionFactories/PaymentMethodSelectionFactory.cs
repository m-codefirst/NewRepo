using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using TRM.Web.Helpers;

namespace TRM.Web.Business.SelectionFactories
{
    public class PaymentMethodSelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var paymentMethodHelper = ServiceLocator.Current.GetInstance<IAmAPaymentMethodHelper>();

            var paymentMethods = paymentMethodHelper.GetAvailablePaymentMethods();

            return paymentMethods.Select(shippingRow => new SelectItem
            {
                Text = shippingRow.SystemKeyword,
                Value = shippingRow.PaymentMethodId.ToString()
            });
        }
    }

    public class CapturePaymentMethodSelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var paymentMethodHelper = ServiceLocator.Current.GetInstance<IAmAPaymentMethodHelper>();

            var paymentMethods = paymentMethodHelper.GetAvailableCapturePaymentMethods();

            return paymentMethods.Select(shippingRow => new SelectItem
            {
                Text = shippingRow.SystemKeyword,
                Value = shippingRow.PaymentMethodId.ToString()
            });
        }
    }
}