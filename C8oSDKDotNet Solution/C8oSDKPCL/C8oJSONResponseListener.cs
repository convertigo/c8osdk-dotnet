using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using Convertigo.SDK.C8oEnum;

namespace Convertigo.SDK.Listeners
{
    public class C8oJsonResponseListener : C8oHttpResponseListener
    {

        public Action<JObject, IDictionary<String, Object>> OnJsonResponse;

        public C8oJsonResponseListener(Action<JObject, IDictionary<String, Object>> onJsonResponse)
        {
            this.OnJsonResponse = onJsonResponse;
        }

        public ResponseType ResponseType
        {
            get 
            {
                return ResponseType.JSON;
            }
        }

        public String OnStreamResponse(Stream streamResponse, IDictionary<String, Object> parameters, Boolean localCacheEnabled)
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

        public void OnStringResponse(String stringResponse, IDictionary<String, Object> parameters)
        {
            this.OnJsonResponse((JObject) C8oTranslator.StringToJson(stringResponse), parameters);
        }
        
    }
}
