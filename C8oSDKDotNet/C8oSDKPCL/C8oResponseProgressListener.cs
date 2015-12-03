using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    internal class C8oResponseProgressListener : C8oResponseListener
    {
        public Action<C8oProgress, IDictionary<string, object>> OnProgressResponse;

        public C8oResponseProgressListener(Action<C8oProgress, IDictionary<string, object>> onProgressResponse)
        {
            OnProgressResponse = onProgressResponse;
        }
    }
}
