using System;
using System.Web.Mvc;
using EPiServer.Core;
using EPiServer.Web;

namespace TRM.Web.Business.Rendering
{
    internal class ContentAreaItemContext : IDisposable
    {
        private readonly ViewDataDictionary _viewData;

        private const string CurrentDisplayOptionKey = "BootstrapContentArea__SelectedDisplayOption";

        public ContentAreaItemContext(ViewDataDictionary viewData, ContentAreaItem contentAreaItem)
        {
            _viewData = viewData;
            var displayOption = contentAreaItem.LoadDisplayOption() ?? new DisplayOption
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Unknown"
            };

            if (!_viewData.ContainsKey(CurrentDisplayOptionKey))
            {
                _viewData.Add(CurrentDisplayOptionKey, displayOption);
            }
            else
            {
                _viewData[CurrentDisplayOptionKey] = displayOption;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _viewData.Remove(CurrentDisplayOptionKey);
        }
    }
}