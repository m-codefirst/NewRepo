using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TRM.Shared.Services
{
    public abstract class WebRequestService
    {
        protected WebResponse GetWebResponse(WebRequest request)
        {
            try
            {
                return request.GetResponse();
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    return wex.Response;
                }
                throw;
            }
        }

        public T Get<T>(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            // Set some reasonable limits on resources used by this request
            request.MaximumAutomaticRedirections = 4;
            request.MaximumResponseHeadersLength = 4;
            // Set credentials to use for this request.
            request.Credentials = CredentialCache.DefaultCredentials;

            var response = GetWebResponse(request);

            if (response == null || response.ContentLength == 0)
                return default(T);

            // Get the stream associated with the response.
            using (var receiveStream = response.GetResponseStream())
            {
                // Pipes the stream to a higher level stream reader with the required encoding format.
                using (var readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(readStream.ReadToEnd());
                }
            }
        }

        protected WebResponse GetWebResponseAsync(WebRequest request, IAsyncResult result)
        {
            try
            {
                return request.EndGetResponse(result);
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    return wex.Response;
                }
                throw;
            }
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            // Set some reasonable limits on resources used by this request
            request.MaximumAutomaticRedirections = 4;
            request.MaximumResponseHeadersLength = 4;
            // Set credentials to use for this request.
            request.Credentials = CredentialCache.DefaultCredentials;

            var response = await Task.Factory.FromAsync(
                request.BeginGetResponse,
                asyncResult => request.EndGetResponse(asyncResult),
                null
            );

            if (response == null || response.ContentLength == 0)
                return default(T);

            // Get the stream associated with the response.
            using (var receiveStream = response.GetResponseStream())
            {
                // Pipes the stream to a higher level stream reader with the required encoding format.
                using (var readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(await readStream.ReadToEndAsync());
                }
            }
        }
    }
}
