using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Convertigo.SDK
{
    public class C8oResponseJsonListener : C8oResponseListener
    {
        public Action<JObject, IDictionary<string, object>> OnJsonResponse;

        public C8oResponseJsonListener(Action<JObject, IDictionary<string, object>> onJsonResponse)
        {
            OnJsonResponse = onJsonResponse;
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
