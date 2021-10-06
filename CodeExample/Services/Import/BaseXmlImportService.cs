using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using EPiServer.Logging;
using TRM.IntegrationServices.Models;
using TRM.IntegrationServices.Models.Import;

namespace TRM.Web.Services.Import
{
    public class BaseXmlImportService
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(BaseXmlImportService));
        protected T TryDeserializeXml<T>(string sourceXml) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            T result = null;
            try
            {
                using (var reader = new StringReader(sourceXml))
                {
                    var resultObject = serializer.Deserialize(reader);
                    result = resultObject as T;
                }
            }
            catch (Exception ex)
            {
                //only log format exception such as wrong Datetime format, number format in xml file
                if (ex.InnerException != null && ex.InnerException.GetType() == typeof(System.FormatException))
                {
                    _logger.Error("Xml Import Service: Format exception when deserialize xml ", ex);
                }
            }

            return result;
        }

        protected ImportXmlResponse ProcessXmlUpdates(string batchId, List<XmlUpdateFromAx> results)
        {
            if (results == null || !results.Any())
            {
                return new ImportXmlResponse
                {
                    XmlUpdates = new List<XmlUpdateFromAx>(),
                    ResponseResult = new TrmResponse
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = $"AxDataImport batch: {batchId}. Empty or XML deserialization error."
                    }
                };

            }

            // Write error logs
            var unsuccessResults = results.Where(x => !x.IsSuccess).ToList();
            if (unsuccessResults.Any())
            {
                var count = 0;
                unsuccessResults.ForEach(item =>
                {
                    if (count < 1)
                    {
                        _logger.Error($"ProcessXmlUpdates failed Export status: {item.ExportStatus} : This message will show only once");
                        _logger.Error(item.Message, item.Exception);
                    }
                    count++;

                });
            }

            return new ImportXmlResponse
            {
                XmlUpdates = results,
                ResponseResult = new TrmResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    ResponseMessage = $"AxDataImport batch: {batchId}."
                }
            };
        }
    }
}