using TRM.Shared.Models.DTOs;
using TRM.Shared.Models.DTOs.Payments;

namespace EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays
{
    /// <summary>
    /// Information used when creating the payment // Bit changes
    /// </summary>
    public class BarclaysCardPaymentDto : BasePaymentDto 
    {
        public string SessionId { get; set; }
        public string NameOnCard { get; set; }
        public bool SaveCard { get; set; }
        public string TokenisedCard { get; set; }
        public CreditCardDto CardToUse { get; set; }
    }
    
}
