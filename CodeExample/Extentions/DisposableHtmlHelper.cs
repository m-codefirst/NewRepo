using System;

namespace TRM.Web.Extentions
{
    public class DisposableHtmlHelper : IDisposable
    {
        private readonly Action _end;

        public DisposableHtmlHelper(Action end)
        {
            _end = end;
        }

        public void Dispose()
        {
            _end();
        }
    }
}