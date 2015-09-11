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
}
