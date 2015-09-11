using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Exceptions
{
    public class C8oHttpException : Exception
    {
        public C8oHttpException(String message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
