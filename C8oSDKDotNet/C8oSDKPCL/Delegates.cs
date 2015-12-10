using System;
using System.Collections.Generic;

namespace Convertigo.SDK
{
    public delegate C8oPromise<T> C8oOnResponse<T>(T response, IDictionary<string, object> parameters);
    public delegate void C8oOnFail(Exception exception, IDictionary<string, object> parameters);
    public delegate void C8oOnProgress(C8oProgress progress);
}
