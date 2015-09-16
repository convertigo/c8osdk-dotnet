using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Exceptions
{
    public class C8oException : Exception
    {
        public C8oException(String message)
            : base(message)
        {
        }

        public C8oException(String message, Exception exception)
            : base(C8oException.FilterMessage(message, exception), C8oException.FilterException(exception))
        {
        }

        private static String FilterMessage(String message, Exception exception)
        {
            if (exception is C8oException)
            {
                message = exception.Message + " | " + message;
            }
            return message;
        }

        private static Exception FilterException(Exception exception)
        {
            /*if (exception is C8oException)
            {
                return null;
            }*/
            return exception;
        }
    }

    public class C8oHttpException : Exception
    {
        public C8oHttpException(String message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    public class C8oRessourceNotFoundException : Exception
    {
        public C8oRessourceNotFoundException(String message) : base(message) { }
        public C8oRessourceNotFoundException(String message, Exception innerException) : base(message, innerException) { }
    }

    public class C8oUnavailableLocalCacheException : Exception
    {

        public C8oUnavailableLocalCacheException(String message) : base(message) { }
        public C8oUnavailableLocalCacheException(String message, Exception innerException) : base(message, innerException) { }
    }
}
