using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Convertigo.SDK.Listeners
{
    public class C8oJsonResponseListener : C8oResponseListener
    {

        public Action<JObject, Dictionary<String, Object>> OnJsonResponse;

        public C8oJsonResponseListener(Action<JObject, Dictionary<String, Object>> onJsonResponse)
        {
            this.OnJsonResponse = onJsonResponse;
        }
    }
}
