namespace TRM.Shared.Interfaces
{
    public interface IControlInventory
    {
        string Code { get; set; }

        bool Sellable { get; set; }

        bool OffSale { get; set; }

        bool SourcedToOrder { get; set; }

        bool TrackInventory { get; set; }

        int LimitedEditionPresentation { get; set; }
        
        decimal? MinQuantity { get; set; }

        decimal? MaxQuantity { get; set; }
    }
}