using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Internal
{
    internal class C8oHttpInterface
    {
        protected C8o c8o;
        CookieContainer cookieContainer;
        private int timeout;

        public C8oHttpInterface(C8o c8o)
        {
            this.c8o = c8o;
            
            timeout = c8o.Timeout;
            
            cookieContainer = new CookieContainer();

            if (c8o.Cookies != null)
            {
                cookieContainer.Add(new Uri(c8o.Endpoint), c8o.Cookies);
            }
        }

        internal virtual void OnRequestCreate(HttpWebRequest request)
        {

        }

        public async Task<HttpWebResponse> HandleRequest(string url, IDictionary<string, object> parameters)
        {
            var request = (HttpWebRequest) HttpWebRequest.Create(url);
            OnRequestCreate(request);

            request.Method = "POST";
            request.Headers["x-convertigo-sdk"] = C8o.GetSdkVersion();
            request.CookieContainer = cookieContainer;

            SetRequestEntity(request, parameters);

            var task = Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request);
            if (timeout > -1)
            {
                var taskWithTimeout = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
                if (taskWithTimeout is Task<WebResponse>)
                {
                    return task.Result as HttpWebResponse;
                }
                throw new Exception("Timeout exception");
            }
            else
            {
                return await task as HttpWebResponse;
            }
        }

        public async Task<HttpWebResponse> HandleC8oCallRequest(string url, IDictionary<string, object> parameters)
        {
            c8o.c8oLogger.LogC8oCall(url, parameters);
            return await HandleRequest(url, parameters);
        }

        /// <summary>
        /// Add a cookie to the cookie store.<br/>
        /// Automatically set the domain and secure flag using the c8o endpoint.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddCookie(string name, string value)
        {
            cookieContainer.Add(new Uri(c8o.Endpoint), new Cookie(name, value));
        }

        public CookieContainer CookieStore
        {
            get { return cookieContainer;  }
        }

        private void SetRequestEntity(HttpWebRequest request, IDictionary<string, object> parameters)
        {
            request.ContentType = "application/x-www-form-urlencoded";

            // And adds to it parameters
            if (parameters != null && parameters.Count > 0)
            {
                string postData = "";

                foreach (KeyValuePair<string, object> parameter in parameters)
                {
                    var value = parameter.Value;

                    if (value is IEnumerable<object>)
                    {
                        foreach (var v in value as IEnumerable<object>)
                        {
                            postData += Uri.EscapeDataString(parameter.Key) + "=" + Uri.EscapeDataString("" + v) + "&";
                        }
                    }
                    else
                    {
                        postData += Uri.EscapeDataString(parameter.Key) + "=" + Uri.EscapeDataString("" + parameter.Value) + "&";
                    }
                }

                postData = postData.Substring(0, postData.Length - 1);

                // postData = "__connector=HTTP_connector&__transaction=transac1&testVariable=TEST 01";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                // First get the request stream before send it (don't use async because of a .net bug for the request)
                var task = Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, request);
                task.Wait();

                using (var entity = task.Result)
                {
                    // Add the post data to the web request
                    entity.Write(byteArray, 0, byteArray.Length);
                }
            }
        }
    }
}
