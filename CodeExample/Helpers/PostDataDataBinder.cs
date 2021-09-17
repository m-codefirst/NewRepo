using System.IO;
using System.Web;
using Newtonsoft.Json;

namespace TRM.Web.Helpers
{
    public static class PostDataDataBinder
    {
        public static T Bind<T>(HttpRequestBase request) where T : new()
        {
            try
            {
                string json;
                using (Stream req = request.InputStream)
                {
                    req.Seek(0, System.IO.SeekOrigin.Begin);
                    json = new StreamReader(req).ReadToEnd();
                }

                var model = JsonConvert.DeserializeObject<T>(json);
                return model;
            }
            catch
            {
                return new T();
            }
        }
    }
}