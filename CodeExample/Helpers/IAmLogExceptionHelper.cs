using System;
using log4net;

namespace TRM.Web.Helpers
{
    public interface IAmLogExceptionHelper
    {
        void LogInnerExceptions(Exception exception, ILog logger);
    }
}
