using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using EPiServer.DataAnnotations;

namespace Vattenfall.Domain.Core.Editor
{
    /// <summary>
    /// Group names for content types and properties
    /// </summary>
    [ExcludeFromCodeCoverage]
    [GroupDefinitions()]
    public static class ExceptionalGroupNames
    {
        [Display(Name = "Referral", Order = -10)]
        public const string ReferralProperties = "ReferralTeaser";
    }
}
