namespace TRM.Shared.Interfaces
{
    public interface IAmTokenisablePayment
    {
        string MastercardSessionId { get; set; }
        bool IsAmexPayment { get; set; }
        string MastercardCardNumber { get; set; }
        string MastercardCardType { get; set; }
        string MastercardCardExpiry { get; set; }
        string TokenisedCardNumber { get; set; }
      
    }
}
