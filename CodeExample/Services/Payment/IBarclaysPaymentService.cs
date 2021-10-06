namespace TRM.Web.Services.Payment
{
    public interface IBarclaysPaymentService
    {
        void Authorization();
        void BeginWebPayment();
        void GetWebPayment();
        void UpdateWebPayment();
        void CancelWebPayment();
        void CreatePaymentToken();
        void UpdatePaymentToken();
    }
}
