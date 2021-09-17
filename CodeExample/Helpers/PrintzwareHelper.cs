using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using EPiServer;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using EPiServer.Web;
using Mediachase.Commerce.Customers;
using Newtonsoft.Json.Linq;
using TRM.Web.Extentions;
using TRM.Web.Models.Pages;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Helpers
{
    public class PrintzwareHelper : IPrintzwareHelper
    {
        private readonly IBlobFactory _blobFactory;
        private readonly IAmPersonalisedDataHelper _personalisedDataHelper;
        private readonly IOrderRepository _orderRepository;
        private readonly CustomerContext _customerContext;

        public PrintzwareHelper(IBlobFactory blobFactory,
            IAmPersonalisedDataHelper personalisedDataHelper, IContentLoader contentLoader, IOrderRepository orderRepository, CustomerContext customerContext)
        {
            _blobFactory = blobFactory;
            _personalisedDataHelper = personalisedDataHelper;

            _orderRepository = orderRepository;
            _customerContext = customerContext;

            var startPage = contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;
            if (startPage == null) return;

            _containerId = startPage.PersonalisationContainerId;
            _apiUrl = startPage.PrintzwareApiUrl;
            _apiKey = startPage.PrintzwareApiKey;
            _clientId = startPage.PrintzwareClientId;
        }
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly string _clientId;
        private readonly string _containerId;

        private const string Tag = "";// We are not currently integrating tags
        private const string ClientCat = "";// We are not currently using the category view
        private const string ProductOptions = "";// We are not currently passing any options 

        public string OpenUrl(string orderId, string code, bool edit = false)
        {
            if (CheckPrintzwareSettings()) return null;
            if (HttpContext.Current?.Request == null) return null;

            var time = DateTime.UtcNow.ToUnixTimeString();

            var postValues = new Dictionary<string, string>
            {
                {"c", _clientId},
                {"t", edit ? Constants.StringConstants.Gifting.EditPrintzwareProductParam : Constants.StringConstants.Gifting.OpenPrintzwareProductParam},
                {"f", "open"},
                {"m", Constants.StringConstants.Gifting.PrintzwareOpenModalMode},
                {"o", orderId},
                {"tag", Tag},
                {"clientCat", ClientCat},
                {"p", code},
                {"ts", time}
            };
            var getData = EncodeUrl(postValues);

            var hashData = string.Join(string.Empty, postValues.Select(x => x.Value));

            var encoding = Encoding.UTF8;
            var keyByte = encoding.GetBytes(_apiKey);
            string clientHash;
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                hmacsha256.ComputeHash(encoding.GetBytes(hashData));
                clientHash = ByteToString(hmacsha256.Hash);
            }
            var protocol = HttpContext.Current.Request.IsSecureConnection ? "https://" : "http://";
            var url = protocol + HttpContext.Current.Request.Url.Host;
            return _apiUrl + "/open.php?h=" + clientHash.ToLower() + "&" + getData + "&url=" + HttpUtility.UrlEncode(url) + "&opt=" + ProductOptions;

        }

        public string GetText(string orderId)
        {
            if (CheckPrintzwareSettings()) return null;

            var postValues = new Dictionary<string, string>
            {
                {"c", _clientId},
                {"t", Constants.StringConstants.Gifting.OpenPrintzwareProductParam},
                {"f", "get_text"},
                {"o", orderId},
                {"ts", DateTime.UtcNow.ToUnixTimeString()}
            };

            var postString = EncodeUrl(postValues);

            var encoding = Encoding.UTF8;
            var keyByte = encoding.GetBytes(_apiKey);
            string clientHash;
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                hmacsha256.ComputeHash(encoding.GetBytes(postString));
                clientHash = (ByteToString(hmacsha256.Hash)).ToLower();
            }

            var objRequest = (HttpWebRequest)WebRequest.Create(_apiUrl + "/api.php");
            objRequest.Method = "POST";
            objRequest.ContentLength = postString.Length;
            objRequest.ContentType = "application/x-www-form-urlencoded";
            objRequest.Headers.Add("clientID: " + _clientId);
            objRequest.Headers.Add("clientHash: " + clientHash);

            // post data is sent as a stream
            StreamWriter myWriter = null;
            myWriter = new StreamWriter(objRequest.GetRequestStream());
            myWriter.Write(postString);
            myWriter.Close();

            // returned values are returned as a stream, then read into a byte array
            var objResponse = (HttpWebResponse)objRequest.GetResponse();

            var postResponse = "";
            using (var responseStream = new StreamReader(objResponse.GetResponseStream()))
            {
                var responseData = JObject.Parse(responseStream.ReadToEnd());
                responseStream.Close();
                var tryGetMessageFromResponse = responseData["variables"]?["message"]?.ToString();
                postResponse = string.IsNullOrEmpty(tryGetMessageFromResponse) ? responseData.ToString() : tryGetMessageFromResponse;
            }

            return postResponse;
        }

        public string GetThumb(string orderId)
        {
            if (CheckPrintzwareSettings()) return null;

            var postValues = new Dictionary<string, string>
            {
                {"c", _clientId},
                {"t", Constants.StringConstants.Gifting.OpenPrintzwareProductParam},
                {"f", "get_thumb"},
                {"o", orderId},
                {"ts", DateTime.UtcNow.ToUnixTimeString()}
            };

            var postString = EncodeUrl(postValues);

            var encoding = Encoding.UTF8;
            var keyByte = encoding.GetBytes(_apiKey);
            string clientHash;
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                hmacsha256.ComputeHash(encoding.GetBytes(postString));
                clientHash = (ByteToString(hmacsha256.Hash)).ToLower();
            }

            var objRequest = (HttpWebRequest)WebRequest.Create(_apiUrl + "/api.php");
            objRequest.Method = "POST";
            objRequest.ContentLength = postString.Length;
            objRequest.ContentType = "application/x-www-form-urlencoded";
            objRequest.Headers.Add("clientID: " + _clientId);
            objRequest.Headers.Add("clientHash: " + clientHash);

            // post data is sent as a stream
            StreamWriter myWriter = null;
            myWriter = new StreamWriter(objRequest.GetRequestStream());
            myWriter.Write(postString);
            myWriter.Close();

            // returned values are returned as a stream, then read into a byte array
            var objResponse = (HttpWebResponse)objRequest.GetResponse();

            if (objResponse.StatusCode != HttpStatusCode.OK) return string.Empty;

            Guid containerGuid;
            if (!Guid.TryParse(_containerId, out containerGuid)) return string.Empty;

            var containerUri = Blob.GetContainerIdentifier(containerGuid);
            var blob = _blobFactory.CreateBlob(containerUri, ".jpg");

            using (var receiveStream = objResponse.GetResponseStream())
            {
                blob.Write(receiveStream);
            }
            _personalisedDataHelper.Create(blob.ID.ToString());

            var fileName = blob.ID.ToString().Split('/').Last();
            return fileName;
        }

        // encodes a secure hash and builds the URL with parameters needed to confirm the order with Printzware using HttpWebRequest
        // returns JSON
        private string ConfirmItem(string orderId, string encryptedData = "", string basketId = "", string quantities = "")
        {
            if (CheckPrintzwareSettings()) return null;

            var time = DateTime.UtcNow.ToUnixTimeString();

            var postValues = new Dictionary<string, string>
            {
                {"c", _clientId},
                {"t", Constants.StringConstants.Gifting.OpenPrintzwareProductParam},
                {"f", "confirm"},
                {"o", orderId},
                {"no", orderId},
                {"bc", string.Empty},
                {"ed", encryptedData},
                {"b", basketId},
                {"q", quantities},
                {"sp", string.Empty},
                {"spo", string.Empty},
                {"ts", time}
            };

            var postString = EncodeUrl(postValues);

            var encoding = Encoding.UTF8;
            var keyByte = encoding.GetBytes(_apiKey);
            string clientHash;
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                hmacsha256.ComputeHash(encoding.GetBytes(postString));
                clientHash = (ByteToString(hmacsha256.Hash)).ToLower();
            }

            var objRequest = (HttpWebRequest)WebRequest.Create(_apiUrl + "/api.php");
            objRequest.Method = "POST";
            objRequest.ContentLength = postString.Length;
            objRequest.ContentType = "application/x-www-form-urlencoded";
            objRequest.Headers.Add("clientID: " + _clientId);
            objRequest.Headers.Add("clientHash: " + clientHash);

            // post data is sent as a stream
            StreamWriter myWriter = null;
            myWriter = new StreamWriter(objRequest.GetRequestStream());
            myWriter.Write(postString);
            myWriter.Close();

            // returned values are returned as a stream, then read into a string
            var objResponse = (HttpWebResponse)objRequest.GetResponse();

            if (objResponse.StatusCode != HttpStatusCode.OK) return string.Empty;

            var postResponse = "";
            using (var responseStream = new StreamReader(objResponse.GetResponseStream()))
            {
                postResponse = responseStream.ReadToEnd();
                responseStream.Close();
            }

            return postResponse;
        }

        private bool CheckPrintzwareSettings()
        {
            return string.IsNullOrEmpty(_apiUrl) || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_clientId);
        }

        public string GetUniqueId(string id)
        {
            //Remove any special chars eg "-" printzware doesn't like these
            return Regex.Replace($"{Guid.NewGuid()}{id}", "[^0-9A-Za-z]+", string.Empty);
        }

        private string EncodeUrl(Dictionary<string, string> postValues)
        {
            return string.Join("&", postValues.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)?.Replace("%2c", "%2C")}"));
        }

        private string ByteToString(byte[] buff)
        {
            var binary = string.Empty;
            for (var i = 0; i < buff.Length; i++)
                binary += buff[i].ToString("X2"); /* hex format */
            return binary;
        }
        public bool ConfirmOrder(IPurchaseOrder po)
        {
            var doSave = false;
            foreach (var lineItem in po.GetAllLineItems())
            {
                if (lineItem.Properties[StringConstants.CustomFields.PersonalisationUniqueId] == null || string.IsNullOrEmpty(lineItem.Properties[StringConstants.CustomFields.PersonalisationUniqueId].ToString()))
                    continue;

                var orderId = lineItem.Properties[StringConstants.CustomFields.PersonalisationUniqueId].ToString();
                string status;
                string details;
                try
                {
                    status = ConfirmItem(orderId, GetShipmentData(po), po.OrderNumber, lineItem.Quantity.ToString("0.00"));
                    details = GetText(orderId);
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                    details = string.Empty;
                }

                //store status to order
                lineItem.Properties[StringConstants.CustomFields.PersonalisationResponse] = status.Substring(0, Math.Min(status.Length, 50));
                lineItem.Properties[StringConstants.CustomFields.PersonalisationDetails] = details.Substring(0, Math.Min(details.Length, 5000));
                doSave = true;
            }

            if (doSave)
            {
                _orderRepository.Save(po);
            }
            
            return true;
        }

        public bool UpdateLineItemPersonalisation(ICart cart, string pwOrderId, string pwEpiserverImageId)
        {
            var lineItems = cart.GetAllLineItems();
            var itemToUpdate = lineItems.FirstOrDefault(x => x.Properties[StringConstants.CustomFields.PersonalisationUniqueId].ToString() == pwOrderId);
            if (null == itemToUpdate) return false;
            itemToUpdate.Properties[StringConstants.CustomFields.PersonalisationImageId] = pwEpiserverImageId;
            _orderRepository.Save(cart);
            return true;
        }


        private string GetShipmentData(IPurchaseOrder po)
        {

            var shipment = po.GetFirstShipment();
            var name = $"{shipment.ShippingAddress.FirstName.Trim()} {shipment.ShippingAddress.LastName.Trim()}";

            if (string.IsNullOrWhiteSpace(name))
            {
                var customer = _customerContext.GetContactById(po.CustomerId);
                name = customer.FullName;
            }

            var values = new Dictionary<string, string>
            {
                { "SHIP_TO_NAME",name },
                { "S_ADDRESS1", shipment.ShippingAddress.Line1 },
                { "S_ADDRESS2", shipment.ShippingAddress.Line2 },
                { "S_ADDRESS3", shipment.ShippingAddress.City },
                { "S_ADDRESS4",  string.Empty},
                { "S_COUNTY", shipment.ShippingAddress.RegionName  },
                { "S_COUNTRY", shipment.ShippingAddress.CountryCode },
                { "S_POSTCODE", shipment.ShippingAddress.PostalCode },
                { "S_POSTAGE", "2" }
            };
            var js = new JavaScriptSerializer();
            var json = js.Serialize(values);
            // create random 16 length iv string
            var iv = Guid.NewGuid().ToString().Substring(0, 16);
            // apiKey needs to be 32 in length, fill with /0

            var apiKey = _apiKey.PadRight(32, '\0');

            var ed = Encrypt(json, apiKey, iv);
            return HttpUtility.UrlEncode(iv + ed);
        }

        private static string Encrypt(string data, string apiKey, string iv)
        {

            var rj = new RijndaelManaged()
            {
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC,
                KeySize = 256,
                BlockSize = 128,
                //FeedbackSize = 256
            };

            var key = Encoding.ASCII.GetBytes(apiKey);

            var encryptor = rj.CreateEncryptor(key, Encoding.ASCII.GetBytes(iv));

            var msEncrypt = new MemoryStream();
            var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            var toEncrypt = Encoding.ASCII.GetBytes(data);

            csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
            csEncrypt.FlushFinalBlock();

            var encrypted = msEncrypt.ToArray();

            return (Convert.ToBase64String(encrypted));
        }

    }
}