using System;

namespace Convertigo.SDK
{
    public class C8oException : Exception
    {
        public C8oException(string message)
            : base(message)
        {
        }

        public C8oException(string message, Exception exception)
            : base(C8oException.FilterMessage(message, exception), C8oException.FilterException(exception))
        {
        }

        private static string FilterMessage(string message, Exception exception)
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
        public C8oHttpException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    public class C8oRessourceNotFoundException : C8oException
    {
        public C8oRessourceNotFoundException(string message) : base(message) { }
        public C8oRessourceNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class C8oCouchbaseLiteException : C8oException
    {
        public C8oCouchbaseLiteException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class C8oUnavailableLocalCacheException : Exception
    {

        public C8oUnavailableLocalCacheException(string message) : base(message) { }
        public C8oUnavailableLocalCacheException(string message, Exception innerException) : base(message, innerException) { }
    }
}
