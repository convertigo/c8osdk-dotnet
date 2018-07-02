using Convertigo.SDK.C8oEnum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Convertigo.SDK.Listeners
{
    public class C8oXmlResponseListener : C8oHttpResponseListener
    {
        public Action<XDocument, IDictionary<String, Object>> OnXmlResponse;

        public C8oXmlResponseListener(Action<XDocument, IDictionary<String, Object>> onXmlResponse)
        {
            this.OnXmlResponse = onXmlResponse;
        }

        public ResponseType ResponseType
        {
            get
            {
                return ResponseType.XML;
            }
        }

        public String OnStreamResponse(Stream streamResponse, IDictionary<String, Object> parameters, Boolean localCacheEnabled)
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

        public void OnStringResponse(String stringResponse, IDictionary<String, Object> parameters)
        {
            this.OnXmlResponse(C8oTranslator.StringToXml(stringResponse), parameters);
        }
    }
}
