using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using TRM.Web.Models.Pages;
using TRM.Web.Models.Settings;

namespace TRM.Web.Services
{
    public class CommonSettingsService : ICommonSettingsService
    {
        protected readonly IContentLoader _contentLoader;

        public CommonSettingsService(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public GBCHSettings GetGBCHSettings()
        {
            var settingsPage = GetSettingsPage<GBCHSettingsPage>(x => x.GBCHSettingsPage);

            if (settingsPage == null)
            {
                // Get obsolete values
                
                var startPage = GetStartPage();
                return new GBCHSettings
                {
                    GBCHLandingPageRef = startPage?.GBCHLandingPage,
                };
            }

            var settings = new GBCHSettings
            {
                GBCHLandingPageRef = settingsPage.GBCHLandingPage,
            };

            return settings;
        }

        private T GetSettingsPage<T>(Func<StartPage, ContentReference> getSettingsPageReference) where T : PageData
        {
            var startPage = GetStartPage();
            if (startPage == null)
            {
                throw new InvalidOperationException("Start page has not been found. As such unable to get settings.");
            }

            var settingsPageRef = getSettingsPageReference(startPage);
            if (ContentReference.IsNullOrEmpty(settingsPageRef))
            {
                return null;
            }
            
            return _contentLoader.Get<T>(settingsPageRef);
        }

        private StartPage GetStartPage()
        {
            return _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
        }
    }
}