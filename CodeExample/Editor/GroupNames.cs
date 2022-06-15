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
    public static class GroupNames
    {

        [Display(Name = "Metadata", Order = 10)]
        public const string MetaData = "Metadata";

        [Display(Name = "Site Settings", Order = 11)]
        public const string SiteSettings = "SiteSettings";

        [Display(Name = "Not In Use", Order = 12)]
        public const string NotInUse = "Not in use";

        [Display(Name = "Configuration", Order = 15)]
        public const string Configuration = "Configuration";

        [Display(Name = "Referral", Order = 30)]
        public const string ReferralProperties = "Referral";

        [Display(Name = "Comparison", Order = 40)]
        public const string CompareProperties = "Comparison";

        [Display(Name = "Security & Search", Order = 100)]
        public const string SecurityAndSearch = "Security & Search";

        [Display(Name = "Vattenfall", Order = 4000)]
        public const string Vattenfall = "Vattenfall";

        [Display(Name = "Vattenfall Form", Order = 4001)]
        public const string VattenfallForm = "VattenfallForm";

        [Display(Name = "Analytics", Order = 9000)]
        public const string Analytics = "Analytics";

        [Display(Name = "SEO Structured Data", Order = 9100)]
        public const string SeoStructuredData = "SEO Structured Data";

        [Display(Name = "Governance", Order = 100000)]
        public const string Governance = "Governance";

        [Display(Name = "Primary Layout", Order = 1)]
        public const string PrimaryLayout = "Primary Layout";

        [Display(Name = "Sales Flow Settings", Order = 1)]
        public const string SalesFlowSettings = "Sales Flow Settings";
    }
}
