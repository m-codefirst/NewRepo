using System;
using System.Linq;
using Mediachase.Commerce.Customers;
using TRM.IntegrationServices.Models.EntityFramework;
using TRM.IntegrationServices.Models.Export.CreditPayments;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs.Payments;

namespace TRM.Web.Helpers
{
    public interface IAccountCreditPaymentHelper
    {
        Guid CreateCreditPayment(ManualPaymentDto paymentDto, CustomerContact customer);
    }
    public class AccountCreditPaymentHelper : IAccountCreditPaymentHelper
    {
        public Guid CreateCreditPayment(ManualPaymentDto paymentDto, CustomerContact customer)
        {
            using (var db = new CustomDBContext())
            {
                var existingPayment = db.CreditPayments.FirstOrDefault(x =>
                    x.PaymentOrderNumber == paymentDto.TransactionId && x.CustomerId == customer.UserId);

                if (existingPayment != null) return existingPayment.CreditPaymentId;

                var lastFour = paymentDto.MastercardCardNumber.PadLeft(4, '0');
                lastFour = lastFour.Substring(lastFour.Length - 4, 4);

                var creditPaymentId = Guid.NewGuid();
                var creditPayment = new CreditPayment
                {
                    CreditPaymentId = creditPaymentId,
                    CreatedDate = DateTime.Now,
                    Amount = paymentDto.Amount,
                    IsAmex = paymentDto.IsAmexPayment,
                    PaymentOrderNumber = paymentDto.TransactionId,
                    Last4Digits = lastFour,
                    Payment3DsId = paymentDto.Mastercard3DSecureId,
                    CustomerId = customer.UserId,
                    AccountId = customer.GetStringProperty(StringConstants.CustomFields.ObsAccountNumber),
                    PaymentSessionId = paymentDto.Mastercard3DsSid,
                    NameOnCard = paymentDto.NameOnCard,
                    TokenId = paymentDto.TokenisedCardNumber,
                    PaymentMethodId = paymentDto.PaymentMethodId.ToString(),
                    SentToiCore = false,
                    SentToiCoreStatus = string.Empty
                };

                db.CreditPayments.Add(creditPayment);
                db.SaveChanges();

                return creditPaymentId;
            }
        }
    }
}