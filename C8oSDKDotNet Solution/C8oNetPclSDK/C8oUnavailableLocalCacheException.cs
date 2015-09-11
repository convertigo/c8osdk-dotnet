using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Exceptions
{
    public class C8oUnavailableLocalCacheException : Exception
    {

        public C8oUnavailableLocalCacheException(String message) : base(message) { }
        public C8oUnavailableLocalCacheException(String message, Exception innerException) : base(message, innerException) { }
        
    }
}
