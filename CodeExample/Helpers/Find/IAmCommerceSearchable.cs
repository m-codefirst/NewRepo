using System;
using System.Collections.Generic;
using EPiServer.Core;
using Hephaestus.ContentTypes.Models.Interfaces;
using TRM.Shared.Interfaces;
using TRM.Web.Models.Interfaces.EntryProperties;

namespace TRM.Web.Helpers.Find
{
    public interface IAmCommerceSearchable : ICanBePersonalised , IContent, IRating, IChangeTrackable
    {
        string DisplayNameForSort { get; }
        Dictionary<string,decimal> ProductPrices { get; }
        DateTime ReleaseDate { get; set; }
        string Category { get; }
        List<int> AllCategoryIds { get; }
        string YearOfIssue { get; set; }
        string YearOfWithdrawal { get; set; }
        bool Restricted { get; set; }
        bool PublishOntoSite { get; set; }
        bool Sellable { get; set; }
        bool ShouldBeOmittedFromSearchResults { get; }
    }
}