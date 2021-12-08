using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace TRM.Shared.Services
{
    public class RestService
    {
        private readonly HttpClient _client;
        public RestService(HttpClient client)
        {
            _client = client;
        }
        public async Task<HttpResponseMessage> Get(string url)
        {
            if (_client != null)
                return await _client.GetAsync(url);
            return null;
        }
        public async Task<TReturn> GetWithFormattedUri<TReturn>(string formattedUri)
        {
            var response = await Get(formattedUri);
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<TReturn>(content);
            return data;
        }
    }
}
