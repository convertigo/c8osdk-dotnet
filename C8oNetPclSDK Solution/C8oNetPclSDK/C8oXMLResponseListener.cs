using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Convertigo.SDK.Listeners
{
    public class C8oXmlResponseListener : C8oResponseListener
    {
        public Action<XDocument, Dictionary<String, Object>> OnXmlResponse;

        public C8oXmlResponseListener(Action<XDocument, Dictionary<String, Object>> onXmlResponse)
        {
            this.OnXmlResponse = onXmlResponse;
        }
    }
}
