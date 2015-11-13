using Convertigo.SDK.C8oEnum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Listeners
{
    public interface C8oResponseListener
    {        
    }

    public interface C8oHttpResponseListener : C8oResponseListener
    {

        ResponseType ResponseType
        {
            get;
        } 

        String OnStreamResponse(Stream streamResponse, IDictionary<String, Object> parameters, Boolean localCacheEnabled);
        void OnStringResponse(String stringResponse, IDictionary<String, Object> parameters);
    }
}
