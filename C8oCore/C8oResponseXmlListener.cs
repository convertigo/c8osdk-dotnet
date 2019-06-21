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
    }
}
