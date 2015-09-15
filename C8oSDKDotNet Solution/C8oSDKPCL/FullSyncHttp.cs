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

            if (username != null && !String.IsNullOrWhiteSpace(username) && password != null && !String.IsNullOrWhiteSpace(password))
            {
                authBasicHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            }
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

        public override Object HandleGetDocumentRequest(String fullSyncDatatbaseName, String docidParameterValue, Dictionary<String, Object> parameters = null)
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
            Dictionary<String, Object> options = new Dictionary<String, Object>();

            foreach (String key in parameters.Keys.ToList())
            {
                Match isUse = reFsUse.Match(key);
                if (isUse.Success)
                {
                    if (isUse.Groups[1].Success)
                    {
                        options.Add(isUse.Groups[1].Value, parameters[key]);
                    }
                    parameters.Remove(key);
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

            JObject postData = new JObject();

            foreach (KeyValuePair<String, Object> kvp in parameters)
            {
                JObject obj = postData;
                String key = kvp.Key;
                String[] paths = key.Split(subkeySeparatorParameterValue.ToCharArray());

                if (paths.Length > 1)
                {

                    for (int i = 0; i < paths.Length - 1; i++)
                    {
                        String path = paths[i];
                        if (obj[path] is JObject)
                        {
                            obj = obj[path] as JObject;
                        }
                        else
                        {
                            obj = (obj[path] = new JObject()) as JObject;
                        }
                    }
                    
                    key = paths[paths.Length - 1];
                }
                obj[key] = JToken.FromObject(kvp.Value);
            }

            postData = applyPolicy(fullSyncDatatbaseName, postData, fullSyncPolicy);
            
            return execute(request, postData);
        }

        private JObject applyPolicy(String fullSyncDatatbaseName, JObject document, FullSyncPolicy fullSyncPolicy)
        {
            if (fullSyncPolicy == FullSyncPolicy.NONE)
            {

            }
            else if (fullSyncPolicy == FullSyncPolicy.CREATE)
            {
                document.Remove("_id");
                document.Remove("_rev");
            }
            else
            {
                String docid = document.GetValue("_id").ToString();

                if (docid != null)
                {
                    if (fullSyncPolicy == FullSyncPolicy.OVERRIDE)
                    {
                        String rev = getDocumentRev(fullSyncDatatbaseName, docid);

                        if (rev != null)
                        {
                            document["_rev"] = rev;
                        }
                    }
                    else if (fullSyncPolicy == FullSyncPolicy.MERGE)
                    {
                        JObject dbDocument = HandleGetDocumentRequest(fullSyncDatatbaseName, docid) as JObject;

                        if (dbDocument.GetValue("_id") != null)
                        {
                            document.Remove("_rev");
                            merge(dbDocument, document);
                            document = dbDocument;
                        }
                    }
                }
            }

            document.Remove("_c8oMeta");

            return document;
        }

        private void merge(JObject jsonTarget, JObject jsonSource)
        {
            foreach (KeyValuePair<String, JToken> kvp in jsonSource)
            {
                try
                {
                    JToken targetValue = jsonTarget.GetValue(kvp.Key);
                    if (targetValue != null)
                    {
                        if (targetValue is JObject && kvp.Value is JObject)
                        {
                            merge(targetValue as JObject, kvp.Value as JObject);
                        }
                        else if (targetValue is JArray && kvp.Value is JArray)
                        {
                            merge(targetValue as JArray, kvp.Value as JArray);
                        }
                        else
                        {
                            jsonTarget.Add(kvp.Key, kvp.Value);
                        }
                    }
                    else
                    {
                        jsonTarget.Add(kvp.Key, kvp.Value);
                    }
                }
                catch (Exception e)
                {
                    //TODO: handle
                }
            }
        }

        private void merge(JArray targetArray, JArray sourceArray)
        {
		    int targetSize = targetArray.Count;
		    int sourceSize = sourceArray.Count;
		
		    for (int i = 0; i < sourceSize; i++)
            {
			    try
                {
				    JToken targetValue = targetSize > i ? targetArray[i] : null;
				    JToken sourceValue = sourceArray[i];
				    if (sourceValue != null && targetValue != null) {
					    if (targetValue is JObject && sourceValue is JObject) {
						    merge(targetValue as JObject, sourceValue as JObject);
					    }
					    if (targetValue is JArray && sourceValue is JArray) {
                            merge(targetValue as JArray, sourceValue as JArray);
					    }
					    else {
						    targetArray[i] = sourceValue;
					    }
				    }
				    else if (sourceValue != null && targetValue == null) {
					    targetArray.Add(sourceValue);
				    }
			    } catch (Exception e) {
				    //TODO: handle
				
			    }
		    }
        }

        public override Object HandleAllDocumentsRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters)
        {
            String uri = handleQuery(getDocumentUrl(fullSyncDatatbaseName, "_all_docs"), parameters);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";

            return execute(request);
        }

        public override Object HandleGetViewRequest(String fullSyncDatatbaseName, String ddocParameterValue, String viewParameterValue, Dictionary<String, Object> parameters)
        {
            String uri = handleQuery(getDocumentUrl(fullSyncDatatbaseName, "_design/" + ddocParameterValue) + "/_view/" + viewParameterValue, parameters);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";

            return execute(request);
        }

        public override Object HandleSyncRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, Listeners.C8oResponseListener c8oResponseListener)
        {
            throw new NotImplementedException();
        }

        public override Object HandleReplicatePullRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, Listeners.C8oResponseListener c8oResponseListener)
        {
            JObject source = new JObject();
            source.Add("url", c8o.GetEndpointPart(1) + "/fullsync" + '/' + fullSyncDatatbaseName);

            CookieCollection cookies = c8o.GetCookies();

            if (cookies.Count > 0)
            {
                JObject headers = new JObject();
                StringBuilder cookieHeader = new StringBuilder();

                foreach (Cookie cookie in c8o.GetCookies())
                {
                    cookieHeader.Append(cookie.Name).Append("=").Append(cookie.Value).Append("; ");
                }

                cookieHeader.Remove(cookieHeader.Length - 2, 2);

                headers.Add("Cookie", cookieHeader.ToString());
                source.Add("headers", headers);
            }

            return postReplicate(source , fullSyncDatatbaseName + "_device", true, false);
        }

        public override Object HandleReplicatePushRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, Listeners.C8oResponseListener c8oResponseListener)
        {
            throw new NotImplementedException();
        }

        private JObject postReplicate(JToken source, JToken target, bool createTarget, bool continuous, bool cancel = false, JArray docIds = null, String proxy = null)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(serverUrl + "/_replicate");
            request.Method = "POST";

            JObject json = new JObject();

            json.Add("source", source);
            json.Add("target", target);
            json.Add("create_target", createTarget);
            json.Add("continuous", continuous);
            json.Add("cancel", cancel);
            
            if (docIds != null)
            {
                json.Add("doc_ids", docIds);
            }

            if (proxy != null)
            {
                json.Add("proxy", proxy);
            }

            return execute(request, json);
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
		    return serverUrl + '/' + db + "_device";
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
		    if (query != null && query.Count > 0)
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
                request.Headers["Authorization"] = authBasicHeader;
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
