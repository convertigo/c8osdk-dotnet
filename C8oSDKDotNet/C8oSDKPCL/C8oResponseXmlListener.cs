using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Convertigo.SDK
{
    public class C8oResponseXmlListener : C8oResponseListener
    {
        public readonly Action<XDocument, IDictionary<string, object>> OnXmlResponse;

        public C8oResponseXmlListener(Action<XDocument, IDictionary<string, object>> onXmlResponse)
        {
            OnXmlResponse = onXmlResponse;
        }
        /*
        public String OnStreamResponse(Stream streamResponse, IDictionary<string, object> parameters, Boolean localCacheEnabled)
        {
            String responseString = null;
            if (localCacheEnabled)
            {
                responseString = C8oTranslator.StreamToString(streamResponse);
                this.OnStringResponse(responseString, parameters);
            }
            else
            {
                this.OnXmlResponse(C8oTranslator.StreamToXml(streamResponse), parameters);
            }
            return responseString;
        }

        public void OnStringResponse(String stringResponse, IDictionary<string, object> parameters)
        {
            this.OnXmlResponse(C8oTranslator.StringToXml(stringResponse), parameters);
        }
        */
    }
}
