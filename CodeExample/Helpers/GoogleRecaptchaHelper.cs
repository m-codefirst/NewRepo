using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using TRM.Shared.Constants;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers
{
    public interface IGoogleRecaptchaHelper
    {
        string SiteKey { get; }
        bool Validate(HttpRequestBase request, out List<string> errors);
        bool Validate(string captcha, out List<string> errors);
        bool IsEnabled();
    }

    [ServiceConfiguration(ServiceType =typeof(IGoogleRecaptchaHelper))]
    public class GoogleRecaptchaHelper : IGoogleRecaptchaHelper
    {
        //SiteKey: 6LdxqKgUAAAAAPMXAwDTNpo8d4k195h5Cn-W1L-R
        //SecretKey:6LdxqKgUAAAAAOHmQsFP2ON5NRwKgwaKG2XfvvWU
        private readonly IContentLoader _contentLoader;
        private readonly LocalizationService _localizationService;

        public GoogleRecaptchaHelper(IContentLoader contentLoader, LocalizationService localizationService)
        {
            _contentLoader = contentLoader;
            _localizationService = localizationService;
        }

        public bool Validate(HttpRequestBase request, out List<string> errors)
        {
            var captcha = request.Form["g-Recaptcha-Response"];
            return Validate(captcha, out errors);
        }

        public bool Validate(string captcha, out List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(captcha))
            {
                errors = new List<string>() { _localizationService.GetStringByCulture(StringResources.RecaptchaRequiredError, "Please verify you're not a robot", ContentLanguage.PreferredCulture) };
                return false;
            }

            var privateKey = _contentLoader.Get<StartPage>(ContentReference.StartPage).RecaptchaSecretKey;

            using (var client = new WebClient())
            {
                var googleReply = client.DownloadString(string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", privateKey, captcha));
                var captchaResponse = JsonConvert.DeserializeObject<RecaptchaResponse>(googleReply);
                errors = captchaResponse.ErrorCodes;
                return captchaResponse.Success;
            }
        }

        public bool IsEnabled() => _contentLoader.Get<StartPage>(ContentReference.StartPage).RecaptchaEnabled;

        public string SiteKey => _contentLoader.Get<StartPage>(ContentReference.StartPage).RecaptchaSiteKey;
    }

    public class RecaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }
}