using Convertigo.SDK.FullSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Convertigo.SDK.Utils;
using Convertigo.SDK.FullSync.Enums;
using Convertigo.SDK.Exceptions;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Convertigo.SDK.Listeners;

namespace Convertigo.SDK
{
    public class FullSyncHttp : FullSyncInterface
    {
        private static Regex reContentType = new Regex("(.*?)\\s*;\\s*charset=(.*?)\\s*", RegexOptions.IgnoreCase);
        private static Regex reFsUse = new Regex("^(?:_use_(.*)$|__)");
        private String serverUrl;
        private C8o c8o;
        private String authBasicHeader;

        public FullSyncHttp(String serverUrl, String username = null, String password = null)
        {
            this.serverUrl = serverUrl;
            /*
            if (username != null && !username.isEmpty() && password != null && !password.isEmpty())
            {
                try
                {
                    authBasicHeader = new BasicScheme().authenticate(new UsernamePasswordCredentials(username, password), forAuth, null);
                }
                catch (AuthenticationException e)
                {
                    //TODO:
                }
            }
             */
        }

        public override void Init(C8o c8o, C8oSettings c8oSettings, String endpointFirstPart)
        {
            base.Init(c8o, c8oSettings, endpointFirstPart);
            
            this.c8o = c8o;
        }

        public override void HandleFullSyncResponse(Object response, Dictionary<String, Object> parameters, Listeners.C8oResponseListener c8oResponseListener)
        {
            if (c8oResponseListener is C8oJsonResponseListener)
            {
                (c8oResponseListener as C8oJsonResponseListener).OnJsonResponse((response as JObject), parameters);
            }
            else if (c8oResponseListener is C8oXmlResponseListener)
            {

            }
            else
            {
                throw new ArgumentException(C8oExceptionMessage.UnknownType("c8oResponseListener", c8oResponseListener));
            }
        }

        public override Object HandleGetDocumentRequest(String fullSyncDatatbaseName, String docidParameterValue, Dictionary<String, Object> parameters)
        {
            String uri = handleQuery(getDocumentUrl(fullSyncDatatbaseName, docidParameterValue), parameters);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";

            return execute(request);
        }

        public override Object handleDeleteDocumentRequest(String fullSyncDatatbaseName, String docidParameterValue, Dictionary<String, Object> parameters)
        {
            parameters = handleRev(fullSyncDatatbaseName, docidParameterValue, parameters);

            String uri = handleQuery(getDocumentUrl(fullSyncDatatbaseName, docidParameterValue), parameters);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "DELETE";

            return execute(request);
        }

        public override Object handlePostDocumentRequest(String fullSyncDatatbaseName, FullSyncPolicy fullSyncPolicy, Dictionary<String, Object> parameters)
        {
            JObject postData = new JObject();
            Dictionary<String, Object> options = new Dictionary<String, Object>();

            foreach (KeyValuePair<String, Object> kvp in parameters)
            {
                Match isUse = reFsUse.Match(kvp.Key);
                if (isUse.Success)
                {
                    if (isUse.Groups[1].Success)
                    {
                        options.Add(isUse.Groups[1].Value, kvp.Value);
                    }
                }
                else
                {
                    postData.Add(kvp.Key, kvp.Value.ToString());
                }
            }

            String uri = handleQuery(getDatabaseUrl(fullSyncDatatbaseName), options);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "POST";

            // Gets the subkey separator parameter
            String subkeySeparatorParameterValue = C8oUtils.PeekParameterStringValue(parameters, FullSyncPostDocumentParameter.SUBKEY_SEPARATOR.name, false);

            if (subkeySeparatorParameterValue == null)
            {
                subkeySeparatorParameterValue = ".";
            }
            
            return execute(request, postData);
        }

        public override Object HandleAllDocumentsRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters)
        {
            throw new NotImplementedException();
        }

        public override Object HandleGetViewRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters)
        {
            throw new NotImplementedException();
        }

        public override Object HandleSyncRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, Listeners.C8oResponseListener c8oResponseListener)
        {
            throw new NotImplementedException();
        }

        public override Object HandleReplicatePullRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, Listeners.C8oResponseListener c8oResponseListener)
        {
            throw new NotImplementedException();
        }

        public override Object HandleReplicatePushRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, Listeners.C8oResponseListener c8oResponseListener)
        {
            throw new NotImplementedException();
        }

        public override Object HandleResetDatabaseRequest(String fullSyncDatatbaseName)
        {
            throw new NotImplementedException();
        }

        public override Object GetResponseFromLocalCache(String c8oCallRequestIdentifier)
        {
            throw new NotImplementedException();
        }

        public override void SaveResponseToLocalCache(String c8oCallRequestIdentifier, String responseString, String responseType, int timeToLive)
        {
            throw new NotImplementedException();
        }
        /*
        private String assertParameter(Dictionary<String, Object> parameters, String parameter)
        {
            String value = C8oUtils.GetParameterStringValue(parameters, parameter, false);

            if (value == null)
            {
                throw new ArgumentException("missing the '" + parameter + "' parameter");
            }

            return value;
        }
        */
        private Dictionary<String, Object> handleRev(String fullSyncDatatbaseName, String docid, Dictionary<String, Object> parameters)
        {
            String rev = C8oUtils.GetParameterStringValue(parameters, FullSyncDeleteDocumentParameter.REV.name, false);
            if (rev == null)
            {
                rev = getDocumentRev(fullSyncDatatbaseName, docid);
                if (rev != null)
                {
                    parameters.Add(FullSyncDeleteDocumentParameter.REV.name, getDocumentRev(fullSyncDatatbaseName, docid));
                }
            }
            return parameters;
        }

        private String getDocumentRev(String fullSyncDatatbaseName, String docid)
        {
            JObject head = headDocument(fullSyncDatatbaseName, docid);
            String rev = null;
            try
            {
                JObject _c8oMeta = head.GetValue("_c8oMeta") as JObject;
                if ("success" == _c8oMeta.GetValue("status").ToString())
                {
                    rev = (_c8oMeta.GetValue("headers") as JObject).GetValue("ETag").ToString();
                    rev = rev.Substring(1, rev.Length - 2);
                }
            }
            catch (Exception e)
            {

            }

            return rev;
        }

        private JObject headDocument(string fullSyncDatatbaseName, string docid)
        {
            String uri = getDocumentUrl(fullSyncDatatbaseName, docid);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "HEAD";

            return execute(request);
        }

        private String getDatabaseUrl(String db)
        {
		    if (String.IsNullOrWhiteSpace(db))
            {
                throw new ArgumentException("blank 'db' not allowed");
		    }
            db = WebUtility.UrlEncode(db);
		    return serverUrl + '/' + db;
	    }
	
	    private String getDocumentUrl(String db, String docid)
        {
            if (String.IsNullOrWhiteSpace(docid))
            {
                throw new ArgumentException("blank 'docid' not allowed");
		    }
		    if (!docid.StartsWith("_design/")) {
                docid = WebUtility.UrlEncode(docid);
		    }
		    return getDatabaseUrl(db) + '/' + docid;
	    }

        private String handleQuery(String url, Dictionary<String, Object> query)
        {
		    StringBuilder uri = new StringBuilder(url);
		    if (query != null || query.Count > 0)
            {
                uri.Append("?");
                foreach (KeyValuePair<String, Object> kvp in query)
                {
                    uri.Append(WebUtility.UrlEncode(kvp.Key)).Append("=").Append(WebUtility.UrlEncode(kvp.Value.ToString())).Append("&");
                }
                uri.Remove(uri.Length - 1, 1);
		    }
		    return uri.ToString();
	    }

        private JObject execute(HttpWebRequest request, JObject document = null)
        {
            if (request.Accept == null)
            {
                request.Accept = "application/json";
            }

            if (authBasicHeader != null)
            {

            }

            if (document != null)
            {
                request.ContentType = "application/json";

                Stream postStream = Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, request).Result;

                // postData = "__connector=HTTP_connector&__transaction=transac1&testVariable=TEST 01";
                byte[] byteArray = Encoding.UTF8.GetBytes(document.ToString());
                // Add the post data to the web request
                
                postStream.Write(byteArray, 0, byteArray.Length);
            }

            HttpWebResponse response;
            try
            {
                response = Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request).Result as HttpWebResponse;
            }
            catch (WebException e)
            {
                response = e.Response as HttpWebResponse;
            }
            catch (Exception e)
            {
                if (e.InnerException is WebException)
                {
                    response = (e.InnerException as WebException).Response as HttpWebResponse;
                }
                else
                {
                    throw new C8oHttpException(C8oExceptionMessage.RunHttpRequest(), e);
                }
            }

            Match matchContentType = reContentType.Match(response.ContentType);

            String contentType;
            String charset;
            if (matchContentType.Success)
            {
                contentType = matchContentType.Groups[1].Value;
                charset = matchContentType.Groups[2].Value;
            }
            else
            {
                contentType = response.ContentType;
                charset = "UTF-8";
            }

            JObject json;

            if (contentType == "application/json" || contentType == "test/plain")
            {
                
                StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(charset));
                String entityContent = streamReader.ReadToEnd();
                try
                {
                    json = JObject.Parse(entityContent);
                }
                catch (Exception e)
                {
                    json = new JObject();
                    try
                    {
                        json.Add("item", JArray.Parse(entityContent));
                    }
                    catch (Exception e2)
                    {
                        json.Add("data", entityContent);
                    }
                }
            }
            else
            {
                json = new JObject();

                if (response.ContentType.StartsWith("text/"))
                {
                    StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(charset));
                    String entityContent = streamReader.ReadToEnd();
                    json.Add("data", entityContent);
                }
                else
                {
                    // TODO base64
                }
            }

            if (json == null)
            {
                json = new JObject();
            }

            JObject c8oMeta = new JObject();
			
			int code = (int) response.StatusCode;
			c8oMeta.Add("statusCode", code);
			
			String status =
					code < 100 ? "unknown" :
					code < 200 ? "informational" :
					code < 300 ? "success" :
					code < 400 ? "redirection" :
					code < 500 ? "client error" :
					code < 600 ? "server error" : "unknown";
			c8oMeta.Add("status", status);
			
			c8oMeta.Add("reasonPhrase", response.StatusDescription);
			
			JObject headers = new JObject();

            foreach (String name in response.Headers.AllKeys)
            {
                headers.Add(name, response.Headers[name]);
            }

            c8oMeta.Add("headers", headers);
			
			json.Add("_c8oMeta", c8oMeta);

            response.Dispose();

            return json;
        }
    }
}
