using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Exceptions
{
    public class C8oRessourceNotFoundException : Exception
    {
        public C8oRessourceNotFoundException(String message) : base(message) { }
        public C8oRessourceNotFoundException(String message, Exception innerException) : base(message, innerException) { }
    }
}
