using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using Convertigo.SDK.C8oEnum;

namespace Convertigo.SDK
{
    public class C8oResponseJsonListener : C8oResponseListener
    {
        public Action<JObject, IDictionary<string, object>> OnJsonResponse;

        public C8oResponseJsonListener(Action<JObject, IDictionary<string, object>> onJsonResponse)
        {
            this.OnJsonResponse = onJsonResponse;
        }
        /*
        public String OnStreamResponse(Stream streamResponse, IDictionary<string, object> parameters, bool localCacheEnabled)
        {
            String responseString = null;
            if (localCacheEnabled)
            {
                responseString = C8oTranslator.StreamToString(streamResponse);
                this.OnStringResponse(responseString, parameters);
            } else 
            {
                this.OnJsonResponse(C8oTranslator.StreamToJson(streamResponse), parameters);
            }
            return responseString;
        }

        public void OnStringResponse(string stringResponse, IDictionary<string, object> parameters)
        {
            this.OnJsonResponse(C8oTranslator.StringToJson(stringResponse) as JObject, parameters);
        }
        */
    }
}
