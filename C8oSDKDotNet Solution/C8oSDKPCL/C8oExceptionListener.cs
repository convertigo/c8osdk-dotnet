using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Listeners
{
    public class C8oExceptionListener
    {
        public readonly Action<Exception> OnException;

        public C8oExceptionListener(Action<Exception> OnException)
        {
            this.OnException = OnException;
        }
    }
}
