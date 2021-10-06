using EPiServer.Framework.Blobs;
using EPiServer.ServiceLocation;
using System;
using System.Globalization;
using Newtonsoft.Json;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Models.EntityFramework.InvoiceStatements;
using TRM.Web.Services.HtmlToPdf;
using TRM.Web.Services.Import;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using EPiServer.Logging.Compatibility;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Services.InvoiceStatements
{
    public interface IBullionDocumentService
    {
        Document GetDocumentById(Guid documentId);
        Task<GbiPdfFileDto> GetPdfFileData(Document document);
        Task<string> DownloadAndSavePdfFilesAsync();
    }

    [ServiceConfiguration(ServiceType = typeof(IBullionDocumentService))]
    public class BullionDocumentService : IBullionDocumentService
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(BullionDocumentService));

        private readonly IBlobFactory _blobFactory;
        private readonly IBullionDocumentRepository _documentRepository;

        private readonly ITrmHtmlToPdf _trmHtmlToPdf;

        private readonly IBullionStatementService _statementService;
        private readonly IBullionInvoiceService _invoiceService;

        public BullionDocumentService(
            IBlobFactory blobFactory,
            IBullionDocumentRepository documentRepository,
            ITrmHtmlToPdf trmHtmlToPdf,
            IBullionStatementService statementService,
            IBullionInvoiceService invoiceService)
        {
            _blobFactory = blobFactory;
            _documentRepository = documentRepository;
            _trmHtmlToPdf = trmHtmlToPdf;
            _statementService = statementService;
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// Download and save pdf file for all records where status is Imported but No Pdf
        /// </summary>
        /// <returns>Number of pdf files downloaded and saved</returns>
        public async Task<string> DownloadAndSavePdfFilesAsync()
        {
            const int batchSize = 100;
            var outstandingDocument = _documentRepository.CountOutstandingDownloadDocument();
            var documentList = _documentRepository.GetDocumentsByStatus(BullionDocumentStatus.ImportedNoPdf, batchSize, BullionDocumentType.Invoice, BullionDocumentType.Statement);
            if (!documentList.Any())
            {
                return "There is no document to download";
            }
            var numberOfConcurrentDownloads = _documentRepository.GetAppropriateStartPageForSiteSpecificProperties().GBINumberOfCurrentDownloads;
            if (numberOfConcurrentDownloads == 0)
            {
                numberOfConcurrentDownloads = 1;
            }

            // Create download tasks
            var throttler = new SemaphoreSlim(initialCount: numberOfConcurrentDownloads);
            var downloadTasks = new List<Task<string>>();
            foreach (var document in documentList)
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await throttler.WaitAsync();
                        var gbiPdfFileDto = await DownloadPdfFromGbi(document);
                        if (gbiPdfFileDto?.Content == null || gbiPdfFileDto.Content.Length == 0)
                        {
                            return $"Unable to download file from URL: {document.PdfPath}";
                        }
                        UpdatePdfPath(document.Id, gbiPdfFileDto.Content, ".pdf", BullionDocumentStatus.ImportedPdf);
                        return string.Empty;
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });

                downloadTasks.Add(task);
            }

            var downloadResults = await Task.WhenAll(downloadTasks);

            var downloadErrors = downloadResults.Where(x => !string.IsNullOrEmpty(x)).ToList();
            var downloadedDocs = documentList.Count - downloadErrors.Count;
            var message = new StringBuilder($"{downloadedDocs} documents downloaded, {outstandingDocument - downloadedDocs} outstanding.");
            if (downloadErrors.Any()) message.Append($"<br><br>Thera are some errors when downloading file. See below and try again.<br>{string.Join("<br>", downloadErrors)}");
            return message.ToString();
        }

        public Document GetDocumentById(Guid documentId)
        {
            return _documentRepository.GetDocumentByDocumentId(documentId);
        }

        public async Task<GbiPdfFileDto> GetPdfFileData(Document document)
        {
            try
            {
                switch (document.Status)
                {
                    case BullionDocumentStatus.ImportedNoPdf:
                        var gbiPdfFileDto = await DownloadPdfFromGbi(document);
                        if (gbiPdfFileDto?.Content == null || gbiPdfFileDto.Content.Length == 0) return null;

                        document = UpdatePdfPath(document.Id, gbiPdfFileDto.Content, ".pdf", BullionDocumentStatus.ImportedPdf);
                        break;
                    case BullionDocumentStatus.DataNoPdf:
                        var generatedPdfData = GenerateDocumentPdfFromAxData(document);
                        if (generatedPdfData == null || generatedPdfData.Length == 0) return null;

                        document = UpdatePdfPath(document.Id, generatedPdfData, ".pdf", BullionDocumentStatus.DataAndPdf);
                        break;
                }

                //Check pdf existing
                if (!string.IsNullOrWhiteSpace(document.PdfPath) && (document.Status == BullionDocumentStatus.ImportedPdf || document.Status == BullionDocumentStatus.DataAndPdf))
                {
                    DateTime dateOfPdf;
                    if (!DateTime.TryParseExact(document.DateOfPdf, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfPdf))
                    {
                        dateOfPdf = DateTime.Now;
                    }

                    var blob = _blobFactory.GetBlob(new Uri(document.PdfPath));
                    var data = blob.ReadAllBytes();
                    if (data != null && data.Length > 0)
                    {
                        return new GbiPdfFileDto
                        {
                            FileName = $"{document.Type}_{dateOfPdf:ddMMyyyy}.pdf",
                            Content = data
                        };
                    }
                }
                // Return empty result
                return null;
            }
            catch (Exception err)
            {
                _logger.Error(err.Message, err);
                return null;
            }
        }

        private async Task<GbiPdfFileDto> DownloadPdfFromGbi(Document document)
        {
            try
            {
                var startPage = _documentRepository.GetAppropriateStartPageForSiteSpecificProperties();
                if (startPage == null || string.IsNullOrEmpty(startPage.GbiOwinUrl) || string.IsNullOrEmpty(startPage.GbiOwinUserName) || string.IsNullOrEmpty(startPage.GbiOwinPassword))
                    throw new ArgumentException("Gbi API settings must be set on the Start Page");

                // GbiOwinUrl: https://qa-migrationapi.bullioninternational.info/api/v1/
                // GbiOwinUserName: migrationApiQA
                // GbiOwinPassword: huvrT2QqWrLeF9x
                var bullionGbiPdfFileService = new BullionGbiPdfFileService(startPage.GbiOwinUrl, startPage.GbiOwinUserName, startPage.GbiOwinPassword);

                var fileData = await bullionGbiPdfFileService.GetFileInfo(document.PdfPath);

                return JsonConvert.DeserializeObject<GbiPdfFileDto>(fileData);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                return null;
            }

        }

        private byte[] GenerateDocumentPdfFromAxData(Document document)
        {
            if (document.Type.Equals(Enums.BullionDocumentType.Invoice.ToString()))
            {
                var reportHtmlString = _invoiceService.GenerateInvoiceDetailReport(document.Id);
                return _trmHtmlToPdf.ConvertHtmlToPdf(reportHtmlString, "Invoice");
            }

            if (document.Type.Equals(Enums.BullionDocumentType.Statement.ToString()))
            {
                var reportHtmlString = _statementService.GenerateStatementDetailReport(document.Id);
                return _trmHtmlToPdf.ConvertHtmlToPdf(reportHtmlString, "Client Statement");
            }

            return null;
        }

        /// <summary>
        /// Update download url so it can be used for next time
        /// </summary>
        private Document UpdatePdfPath(Guid documentId, byte[] data, string extension, string documentStatus)
        {
            try
            {
                var filePath = SaveFile(data, extension);
                return _documentRepository.UpdatePdfPathAndDocumentStatus(documentId, filePath, documentStatus);
            }
            catch (Exception ex)
            {
                _logger.Error($"UpdatePdfPath: {ex.Message}", ex);
                return null;
            }
        }

        private string SaveFile(byte[] data, string extension)
        {
            var startPage = _documentRepository.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || string.IsNullOrEmpty(startPage.PdfBlobContainerId))
                throw new ArgumentException("PdfBlobContainerId setting must be setting in Start Page");

            // Define a container
            var container = Blob.GetContainerIdentifier(new Guid(startPage.PdfBlobContainerId));

            // Uploading a file to blob
            var blob = _blobFactory.CreateBlob(container, extension);
            blob.WriteAllBytes(data);

            return blob.ID.ToString();
        }
    }
}