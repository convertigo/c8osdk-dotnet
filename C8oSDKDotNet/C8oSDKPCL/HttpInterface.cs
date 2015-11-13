using Convertigo.SDK.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Http
{
    public class HttpInterface
    {

        private int timeout;

        public HttpInterface(C8oSettings c8oSettings)
        {
            this.timeout = c8oSettings.timeout;
        }

        public async Task<WebResponse> HandleRequest(String url, Dictionary<String, Object> parameters, CookieContainer cookieContainer = null)
        {
            // Initializes the HTTP request
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            webRequest.CookieContainer = cookieContainer;
            
            // First get the request stream before send it
            Stream postStream;
            try
            {
                Task<Stream> beginGetRequestStreamTask = Task<Stream>.Factory.FromAsync(webRequest.BeginGetRequestStream, webRequest.EndGetRequestStream, webRequest);
                Boolean finished = beginGetRequestStreamTask.Wait(this.timeout);
                if (finished)
                {
                    postStream = beginGetRequestStreamTask.Result;
                }
                else
                {
                    throw new C8oException(C8oExceptionMessage.ToDo());
                }
                // postStream = await Task<Stream>.Factory.FromAsync(webRequest.BeginGetRequestStream, webRequest.EndGetRequestStream, webRequest);
            }
            catch (Exception e)
            {
                throw new C8oHttpException(C8oExceptionMessage.RunHttpRequest(), e);
            }
            // And adds to it parameters
            if (parameters != null && parameters.Count > 0)
            {
                String postData = "";
                KeyValuePair<String, Object> item = parameters.ElementAt(0);
                postData += item.Key + "=" + item.Value;

                for (int i = 1; i < parameters.Count; i++)
                {
                    item = parameters.ElementAt(i);
                    postData += "&" + item.Key + "=" + item.Value;
                }

                // postData = "__connector=HTTP_connector&__transaction=transac1&testVariable=TEST 01";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                // Add the post data to the web request
                postStream.Write(byteArray, 0, byteArray.Length);
            }
            postStream.Dispose();

            // Then get the response stream
            WebResponse response = null;
            try
            {
                Task<WebResponse> task = Task<WebResponse>.Factory.FromAsync(webRequest.BeginGetResponse, webRequest.EndGetResponse, webRequest);
                Boolean finished = task.Wait(this.timeout);
                if (finished)
                {
                    response = task.Result;
                }
                else
                {
                    throw new C8oException(C8oExceptionMessage.ToDo());
                }
                // response = await Task<WebResponse>.Factory.FromAsync(webRequest.BeginGetResponse, webRequest.EndGetResponse, webRequest);
            }
            catch (Exception e)
            {
                throw new C8oHttpException(C8oExceptionMessage.RunHttpRequest(), e);
            }
              
            return response;
        }
    }
}
