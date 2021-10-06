using System;
using System.ComponentModel;
using EPiServer.Core;

namespace TRM.Web.Services.Reporting
{
    public interface IStockReportService
    {
        CustomReportDto<StockReportRow> GetFullReport();

        CustomReportDto<StockReportRow> GetReport(string term, int page, int itemsPerPage, bool @ascending, string sortField);
    }

    public class StockReportRow
    {
        [DisplayName("SKU")]
        public string Sku { get; set; }

        [DisplayName("Product Title")]
        public string ProductTitle { get; set; }

        public string Brand { get; set; }

        [DisplayName("Warehouse code")]
        public string WarehouseCode { get; set; }

        public string Location { get; set; }

        [DisplayName("In Stock")]
        public double InStock { get; set; }        
        
        [DisplayName("Backorder Availability")]
        public DateTime? BackorderAvailability { get; set; }    
        
        [DisplayName("Backorder Quantity")]
        public double BackorderQuantity { get; set; }  
        
        [DisplayName("Preorder Availability")]
        public DateTime? PreorderAvailability { get; set; }    
        
        [DisplayName("Preorder Quantity")]
        public double PreorderQuantity { get; set; }
        
        [DisplayName("Reorder Min. Quantity")]
        public double ReorderMinQuantity { get; set; }   
        
        [DisplayName("Is Tracked")]
        public bool IsTracked { get; set; }

        [DisplayName("Purchase Availability")]
        public DateTime PurchaseAvailability { get; set; } 
                
        [DisplayName("Published On Site")]
        public bool PublishedOnSite { get; set; }        
        
        [DisplayName("Sellable")]
        public bool Sellable { get; set; }      
        
        [DisplayName("Restricted")]
        public bool Restricted { get; set; }        

        [DisplayName("Off Sale")]
        public bool OffSale { get; set; }

        [DisplayName("ContentLink")]
        public ContentReference ContentLink { get; set; }
    }
}