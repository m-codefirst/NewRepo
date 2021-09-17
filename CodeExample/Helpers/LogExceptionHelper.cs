using System;
using log4net;

namespace TRM.Web.Helpers
{
    public class LogExceptionHelper: IAmLogExceptionHelper
    {
        void IAmLogExceptionHelper.LogInnerExceptions(Exception exception, ILog logger)
        {
            var innerException = exception.InnerException;
            while (innerException != null)
            {
                logger.Error(innerException.Message, innerException.InnerException);
                innerException = innerException.InnerException;
            }
        }
    }
}