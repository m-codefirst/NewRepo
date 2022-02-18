using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Extensions;

namespace EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays.Extensions
{
    public static class PaymentExtensions
    {
        public static Orders.BarclaysCardPayment ConvertToBarclaysPayment(this IPayment serializablePayment)
        {
            if (serializablePayment == null) return null;

            var mastercardPayment = ServiceLocator.Current.GetInstance<Orders.BarclaysCardPayment>();

            mastercardPayment.Amount = serializablePayment.Amount;
            mastercardPayment.AuthorizationCode = serializablePayment.AuthorizationCode;
            ((IPayment)mastercardPayment).BillingAddress = serializablePayment.BillingAddress;
            mastercardPayment.CustomerName = serializablePayment.CustomerName;
            mastercardPayment.PaymentMethodId = serializablePayment.PaymentMethodId;
            mastercardPayment.PaymentMethodName = serializablePayment.PaymentMethodName;
            mastercardPayment.PaymentType = serializablePayment.PaymentType;
            mastercardPayment.ProviderTransactionID = serializablePayment.ProviderTransactionID;
            mastercardPayment.Status = serializablePayment.Status;
            mastercardPayment.TransactionID = serializablePayment.TransactionID;
            mastercardPayment.TransactionType = serializablePayment.TransactionType;
            mastercardPayment.ValidationCode = serializablePayment.ValidationCode;
            mastercardPayment.CopyPropertiesWithOverwriteFrom(serializablePayment);

            return mastercardPayment;
        }
    }
}
