using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.Linq;
using EPiServer.ServiceLocation;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.InvoiceStatements;
using static TRM.Web.Constants.Enums;
using Dapper;

namespace TRM.Web.Business.DataAccess
{
    public interface IBullionDocumentRepository
    {
        Document GetDocumentByDocumentId(Guid documentId);
        Document GetStatementById(Guid statementId);
        IQueryable<Document> GetDocumentsByCustomerId(Guid customerId);
        IQueryable<Document> GetDocumentsByCustomerId(Guid customerId, int year);
        Document UpdatePdfPathAndDocumentStatus(Guid documentId, string pdfPath, string documentStatus);
        Document ImportDocumentFromGbi(IGbiPdfDocument gbiDocument, Guid customerId);
        void ImportDocumentFromAx(Document document);
        List<Document> GetDocumentsByStatus(string status, int numberOfRowsToReturn, params BullionDocumentType[] documentTypes);
        int CountOutstandingDownloadDocument();
        bool IsIndicativeMetalPricesStoredForStatement(DateTime statementDate);
    }

    [ServiceConfiguration(typeof(IBullionDocumentRepository), Lifecycle = ServiceInstanceScope.Transient)]
    public class BullionDocumentRepository : DbContextDisposable<BullionInvoiceStatementDbContext>, IBullionDocumentRepository
    {
        public List<Document> GetDocumentsByStatus(string status, int numberOfRowsToReturn, params BullionDocumentType[] documentTypes)
        {
            var strDocumentTypes = documentTypes.Select(x => x.ToString()).ToList();
            return context.Documents
                .Where(x => strDocumentTypes.Contains(x.Type) && x.Status == status)
                .OrderByDescending(x => x.Date)
                .Take(numberOfRowsToReturn)
                .AsNoTracking()
                .ToList();
        }

        public int CountOutstandingDownloadDocument()
        {
            return context.Documents.Count(x => x.Status == BullionDocumentStatus.ImportedNoPdf);
        }

        //Query documents by customer Id
        public IQueryable<Document> GetDocumentsByCustomerId(Guid customerId)
        {
            return context.Documents.Where(x => x.CustomerId == customerId).AsNoTracking();
        }

        //Query documents by customer Id and year
        public IQueryable<Document> GetDocumentsByCustomerId(Guid customerId, int year)
        {
            return GetDocumentsByCustomerId(customerId)
                .Where(x => x.Date.Year.Equals(year)).AsNoTracking();
        }

        //Query document by document id
        public Document GetDocumentByDocumentId(Guid documentId)
        {
            var document = context.Documents.AsNoTracking().FirstOrDefault(x => x.Id == documentId);
            return document;
        }

        public Document GetStatementById(Guid statementId)
        {
            var statement = context.Documents.FirstOrDefault(x => x.Id == statementId);
            if (statement != null)
            {
                statement.StatementIndicativePrices =
                    context.StatementIndicativePrices.Where(x => 
                        x.StatementDate.Day == statement.ToDate.Day &&
                        x.StatementDate.Month == statement.ToDate.Month &&
                        x.StatementDate.Year == statement.ToDate.Year).AsNoTracking().ToList();

                statement.StatementHeader =
                    context.StatementHeaders.FirstOrDefault(x => x.DocumentId.Equals(statement.Id));
                statement.StatementHolding =
                    context.StatementHoldings.FirstOrDefault(x => x.DocumentId.Equals(statement.Id));
                statement.StatementTransactions =
                    context.StatementTransactions.Where(x => x.DocumentId.Equals(statement.Id)).AsNoTracking().ToList();
                statement.StatementVaultItems =
                    context.StatementVaultItems.Where(x => x.DocumentId.Equals(statement.Id)).AsNoTracking().ToList();
            }

            return statement;
        }

        public Document UpdatePdfPathAndDocumentStatus(Guid documentId, string pdfPath, string documentStatus)
        {
            //var entity = context.Documents.FirstOrDefault(x => x.Id == documentId);
            //if (entity != null)
            //{
            //    entity.PdfPath = pdfPath;
            //    entity.DateOfPdf = DateTime.Now.ToString("dd-MM-yyyy");
            //    entity.Status = BullionDocumentStatus.DataAndPdf;
            //    context.SaveChanges();
            //}

            // Truong: The above code doesn't save DateOfPdf and Status on DXC, not sure why yet.
            // Work-around(TEMP): To close ticket, will take look again later.
            var connectionString = ConfigurationManager.ConnectionStrings[Shared.Constants.StringConstants.BullionCustomDatabaseName].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                var dateOfPdf = DateTime.Now.ToString("dd-MM-yyyy");
                conn.Execute("UPDATE custom_Documents SET PdfPath = @pdfPath, DateOfPdf = @dateOfPdf, Status = @status WHERE Id = @id",
                    new
                    {
                        id = documentId,
                        pdfPath = pdfPath,
                        dateOfPdf = dateOfPdf,
                        status = documentStatus
                    });

                return GetDocumentByDocumentId(documentId);
            }
        }

        public Document ImportDocumentFromGbi(IGbiPdfDocument gbiDocument, Guid customerId)
        {
            var document = new Document
            {
                Status = BullionDocumentStatus.ImportedNoPdf,
                PdfPath = gbiDocument.PdfFile,
                DateOfPdf = DateTime.Now.ToString("dd-MM-yyyy"),
                FromDate = gbiDocument.DateFrom,
                ToDate = gbiDocument.DateTo,
                Type = gbiDocument.FileType,
                Date = gbiDocument.DocumentDate,
                CustomerId = customerId,
                Id = Guid.NewGuid()
            };

            context.Documents.AddOrUpdate(document);
            context.SaveChanges();

            return document;
        }

        public void ImportDocumentFromAx(Document document)
        {
            context.Documents.Add(document);
            context.SaveChanges();

            // Store historic data
            if (document.StatementIndicativePrices?.Count > 0)
            {
                context.BulkInsert(document.StatementIndicativePrices, 100);
            }
        }

        public bool IsIndicativeMetalPricesStoredForStatement(DateTime statementDate)
        {
            // Three currencies
            return context.StatementIndicativePrices.Count(x => DbFunctions.TruncateTime(x.StatementDate) == statementDate.Date) >= 3;
        }
    }
}