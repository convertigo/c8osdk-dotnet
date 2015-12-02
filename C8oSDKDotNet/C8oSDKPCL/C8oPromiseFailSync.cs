using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public interface C8oPromiseFailSync<T> : C8oPromiseSync<T>
    {
        C8oPromiseSync<T> Fail(C8oOnFail c8oOnFail);
        C8oPromiseSync<T> FailUI(C8oOnFail c8oOnFail);
    }
}
