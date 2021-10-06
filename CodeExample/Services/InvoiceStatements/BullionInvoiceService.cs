using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Web.Models.EntityFramework.InvoiceStatements;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using EPiServer.ServiceLocation;
using TRM.Shared.Extensions;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Facades;
using TRM.Web.Helpers;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.ViewModels.Invoices;
using TRM.Web.Utils;
using Document = TRM.Web.Models.EntityFramework.InvoiceStatements.Document;
using Enums = TRM.Web.Constants.Enums;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Services.InvoiceStatements
{
    public interface IBullionInvoiceService
    {
        string GenerateInvoiceDetailReport(Guid invoiceId);
        bool AddCustomerInvoiceFromImportData(AxImportData.InvoicesInvoice invoice, Guid customerId);
    }

    [ServiceConfiguration(ServiceType = typeof(IBullionInvoiceService))]
    public class BullionInvoiceService : IBullionInvoiceService
    {
        private readonly CustomerContextFacade _customerContext;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionInvoiceService(CustomerContextFacade customerContext, IAmBullionContactHelper bullionContactHelper)
        {
            _customerContext = customerContext;
            _bullionContactHelper = bullionContactHelper;
        }

        public string GenerateInvoiceDetailReport(Guid invoiceId)
        {
            return MvcUtil.RenderPartialViewToString("InvoicePdfTemplate", GetInvoiceHeaderViewModel(invoiceId));
        }

        public bool AddCustomerInvoiceFromImportData(AxImportData.InvoicesInvoice invoice, Guid customerId)
        {
            DateTime invoiceDate, fromDate, toDate;

            if (!invoice.InvoiceDate.TryParseSqlDatetimeExact(out invoiceDate))
            {
                throw new ArgumentException($"Can not parse {nameof(invoice.InvoiceDate)} field of InvoiceHeader to Datetime.");
            }

            if (!invoice.InvoicePeriodFrom.TryParseSqlDatetimeExact(out fromDate))
            {
                fromDate = new DateTime(invoiceDate.Year, invoiceDate.Month, 1);
            }

            if (!invoice.InvoicePeriodTo.TryParseSqlDatetimeExact(out toDate))
            {
                toDate = fromDate.AddMonths(1).AddDays(-1);
            }

            var invoiceDocument = new Document()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Type = Enums.BullionDocumentType.Invoice.ToString(),
                Date = invoiceDate,
                FromDate = fromDate,
                ToDate = toDate,
                Status = BullionDocumentStatus.DataNoPdf
            };

            var totalAmount = invoice.TotalAmount.ToDecimalExactCulture();
            var totalVat = invoice.TotalVAT.ToDecimalExactCulture();
            var invoiceHeader = new InvoiceHeader
            {
                DocumentId = invoiceDocument.Id,
                CustomerId = customerId,
                InvoiceTotal = totalAmount + totalVat,
                InvoiceTotalExVat = totalAmount,
                Vat = totalVat
            };

            var invoiceDetails = new List<InvoiceDetail>();
            foreach (var invoiceLine in invoice.Lines)
            {
                if (!invoiceLine.DateFrom.TryParseSqlDatetimeExact(out fromDate)) continue;
                if (!invoiceLine.DateTo.TryParseSqlDatetimeExact(out toDate)) continue;
                invoiceDetails.Add(new InvoiceDetail()
                {
                    Id = Guid.NewGuid(),
                    InvoiceHeaderId = invoiceHeader.DocumentId,
                    From = fromDate,
                    To = toDate,
                    TotalExVat = invoiceLine.Amount.ToDecimalExactCulture(),
                    Vat = invoiceLine.VAT.ToDecimalExactCulture(),
                    VariantCode = invoiceLine.ProductId,
                    InvoiceLineNumber = invoiceLine.Description
                });
            }

            if (invoiceDetails.Any())
            {
                invoiceDocument.FromDate = invoiceDetails.Min(x => x.From);
                invoiceDocument.ToDate = invoiceDetails.Max(x => x.To);
            }

            return SaveBullionInvoice(invoiceDocument, invoiceHeader, invoiceDetails);
        }

        private bool SaveBullionInvoice(Document document, InvoiceHeader invoiceHeader, IEnumerable<InvoiceDetail> invoiceDetails)
        {
            using (var newDbContext = new BullionInvoiceStatementDbContext())
            {
                using (var transaction = newDbContext.Database.BeginTransaction())
                {
                    try
                    {
                        newDbContext.Documents.AddOrUpdate(document);
                        newDbContext.InvoiceHeaders.AddOrUpdate(invoiceHeader);
                        newDbContext.InvoiceDetails.AddRange(invoiceDetails);
                        
                        newDbContext.SaveChanges();

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }

                return true;
            }
        }

        private InvoiceHeaderViewModel GetInvoiceHeaderViewModel(Guid documentId)
        {
            var invoiceHeader = GetInvoiceHeaderDetail(documentId);

            var customer = _customerContext.GetContactById(invoiceHeader.CustomerId);
            var currency = _bullionContactHelper.GetDefaultCurrencyCode(customer);
            var customerAddress = _bullionContactHelper.GetBullionAddress(customer);

            var viewModel = new InvoiceHeaderViewModel
            {
                Currency = currency,
                CustomerName = _bullionContactHelper.GetFullname(customer),
                AddressLine1 = customerAddress?.Line1,
                AddressLine2 = customerAddress?.Line2,
                Postcode = customerAddress?.PostalCode,
                Country = customerAddress?.CountryName,
                Account = customer?.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber),
                Period = $"{invoiceHeader.Document.FromDate.ToCommonUKFormat()} - {invoiceHeader.Document.ToDate.ToCommonUKFormat()}",

                InvoicedDate = invoiceHeader.Document.Date,
                InvoiceTotal = invoiceHeader.InvoiceTotal,
                InvoiceTotalExVat = invoiceHeader.InvoiceTotalExVat,
                Vat = invoiceHeader.Vat,
                InvoiceDetails = invoiceHeader.InvoiceDetails
                    .GroupBy(x => new { x.VariantCode, FromDate = x.From.Date, ToDate = x.To.Date })
                    .Select(x => new InvoiceDetailViewModel
                    {
                        Product = x.Key.VariantCode.GetVariantByCode()?.DisplayName,
                        From = x.Key.FromDate,
                        To = x.Key.ToDate,
                        TotalExVat = x.Sum(y=>y.TotalExVat),
                        Vat = x.Sum(y => y.Vat),
                        TotalFee = x.Sum(y => y.TotalExVat) + x.Sum(y => y.Vat)
                    }).Where(x=>!string.IsNullOrEmpty(x.Product)).OrderBy(x=>x.Product).ThenBy(x=>x.From).ToList()
            };

            return viewModel;
        }

        private InvoiceHeader GetInvoiceHeaderDetail(Guid id)
        {
            using (var db = new BullionInvoiceStatementDbContext())
            {
                return db.InvoiceHeaders
                    .Include(ih => ih.InvoiceDetails)
                    .Include(ih => ih.Document)
                    .FirstOrDefault(x => x.DocumentId == id);
            }
        }

        
    }
}