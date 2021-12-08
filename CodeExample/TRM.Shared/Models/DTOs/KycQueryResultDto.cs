using System.ComponentModel;

namespace TRM.Shared.Models.DTOs
{
    public enum AccountKycStatus
    {
        [Description("Submitted")]
        Submitted = 6, // (Automatic KYC check not yet initiated)
        [Description("Pending Additional Info")]
        PendingAdditionalInformation = 1, //(REFERed by GBG)
        [Description("Ready for Review")]
        ReadyForReview = 2, // (Documents submitted, but rejected by GBG- Customer services to review and approve or reject)
        [Description("Rejected")]
        Rejected = 3, // (ALERTed by GBG ROYAL MINT to review as might not be the same)
        [Description("Approved")]
        Approved = 4, // (PASSed by GBG)
        [Description("Need Recheck")]
        NeedReCheck = 5
    }

    public class KycQueryResultDto
    {
        public AccountKycStatus Status { get; set; }
        public string Id3Response { get; set; }
    }
}