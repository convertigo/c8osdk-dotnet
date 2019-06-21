using System;
using System.Collections.Generic;

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
