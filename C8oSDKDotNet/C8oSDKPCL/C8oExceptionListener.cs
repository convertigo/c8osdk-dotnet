using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public class C8oExceptionListener
    {
        public readonly Action<Exception, IDictionary<string, object>> OnException;

        public C8oExceptionListener(Action<Exception, IDictionary<string, object>> OnException)
        {
            this.OnException = OnException;
        }
    }
}
