using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Convertigo.SDK
{
    public delegate C8oPromise<T> C8oOnResponse<T>(T response, IDictionary<string, object> parameters);
    public delegate void C8oOnFail(Exception exception, IDictionary<string, object> parameters);
    public delegate void OnXmlResponse(XDocument response, IDictionary<string, object> parameters);
    public delegate void OnJsonResponse(JObject response, IDictionary<string, object> parameters);
}
