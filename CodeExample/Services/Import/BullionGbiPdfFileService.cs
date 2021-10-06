using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TRM.Web.Services.Import
{
    public class GbiPdfFileDto
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }
    }

    public class TokenResponse
    {
        public string Raw { get; protected set; }
        public JObject Json { get; protected set; }

        public TokenResponse(string raw)
        {
            Raw = raw;
            Json = JObject.Parse(raw);
        }

        public TokenResponse(HttpStatusCode statusCode, string reason)
        {
            IsHttpError = true;
            HttpErrorStatusCode = statusCode;
            HttpErrorReason = reason;
        }

        public bool IsHttpError { get; }

        public HttpStatusCode HttpErrorStatusCode { get; }

        public string HttpErrorReason { get; }

        public string AccessToken
        {
            get { return GetStringOrNull("access_token"); }
        }

        public string IdentityToken
        {
            get { return GetStringOrNull("id_token"); }
        }

        public string Error
        {
            get { return GetStringOrNull("error"); }
        }

        public bool IsError
        {
            get { return (IsHttpError || !string.IsNullOrWhiteSpace(GetStringOrNull("error"))); }
        }

        public long ExpiresIn
        {
            get { return GetLongOrNull("expires_in"); }
        }

        public string TokenType
        {
            get { return GetStringOrNull("token_type"); }
        }

        public string RefreshToken
        {
            get { return GetStringOrNull("refresh_token"); }
        }

        protected virtual string GetStringOrNull(string name)
        {
            JToken value;
            if (Json.TryGetValue(name, out value))
            {
                return value.ToString();
            }

            return null;
        }

        protected virtual long GetLongOrNull(string name)
        {
            JToken value;
            if (Json.TryGetValue(name, out value))
            {
                long longValue = 0;
                if (long.TryParse(value.ToString(), out longValue))
                {
                    return longValue;
                }
            }

            return 0;
        }
    }
    public abstract class OAuthRestService
    {
        private string _baseAddress;
        private string _userName;
        private string _password;

        public OAuthRestService(string baseAddress, string userName, string password)
        {
            _baseAddress = baseAddress;
            _userName = userName;
            _password = password;
        }

        protected async Task<TokenResponse> GetToken()
        {
            var form = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", _userName },
                { "password", _password }
            };
            var client = new HttpClient();

            client.BaseAddress = new Uri(_baseAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync("token", new FormUrlEncodedContent(form));

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                return new TokenResponse(content);
            }
            else
            {
                return new TokenResponse(response.StatusCode, response.ReasonPhrase);
            }
        }

        protected async Task<string> GetContent(string url, TokenResponse token)
        {
            var client = new HttpClient();

            client.BaseAddress = new Uri(_baseAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await client.GetAsync(url);
            var stringResponse = await response.Content.ReadAsStringAsync();
            return stringResponse;
        }
    }
    public class BullionGbiPdfFileService : OAuthRestService
    {
        public BullionGbiPdfFileService(string url, string username, string password)
            : base(url, username, password)
        { }

        public async Task<string> GetFileInfo(string url)
        {
            var token = await GetToken();
            return await GetContent(url, token);
        }
    }
}