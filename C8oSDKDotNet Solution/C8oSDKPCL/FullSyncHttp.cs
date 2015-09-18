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

        public override void HandleFullSyncResponse(Object response, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            if (response is JObject)
            {
                JObject json = response as JObject;
                if (c8oResponseListener is C8oJsonResponseListener)
                {
                    (c8oResponseListener as C8oJsonResponseListener).OnJsonResponse(json, parameters);
                }
                else if (c8oResponseListener is C8oXmlResponseListener)
                {
                    (c8oResponseListener as C8oXmlResponseListener).OnXmlResponse(FullSyncTranslator.FullSyncJsonToXml(json), parameters);
                }
                else
                {
                    throw new ArgumentException(C8oExceptionMessage.UnknownType("c8oResponseListener", c8oResponseListener));
                }
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
                        options[isUse.Groups[1].Value] = parameters[key];
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
                String docid = document["_id"].ToString();

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

                        if (dbDocument["_id"] != null)
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
                    JToken targetValue = jsonTarget[kvp.Key];
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
                            jsonTarget[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        jsonTarget[kvp.Key] = kvp.Value;
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

        public override Object HandleSyncRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            Task.Run(() =>
            {
                HandleReplicatePushRequest(fullSyncDatatbaseName, parameters, c8oResponseListener);
            });
            return HandleReplicatePullRequest(fullSyncDatatbaseName, parameters, c8oResponseListener);
        }

        public override Object HandleReplicatePullRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            return postReplicate(fullSyncDatatbaseName, parameters, c8oResponseListener, true);
        }

        public override Object HandleReplicatePushRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            return postReplicate(fullSyncDatatbaseName, parameters, c8oResponseListener, false);
        }

        private Object postReplicate(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener, bool isPull)
        {
            bool createTarget = true;
            bool continuous = false;
            bool cancel = false;

            if (parameters.ContainsKey("create_target"))
            {
                createTarget = parameters["create_target"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            if (parameters.ContainsKey("continuous"))
            {
                continuous = parameters["continuous"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            if (parameters.ContainsKey("cancel"))
            {
                continuous = parameters["cancel"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            JToken local = fullSyncDatatbaseName + "_device";
            JObject remote = new JObject();

            remote["url"] = c8o.GetEndpointPart(1) + "/fullsync" + '/' + fullSyncDatatbaseName + '/';

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

                headers["Cookie"] = cookieHeader.ToString();
                remote["headers"] = headers;
            }
            
            HttpWebRequest request = HttpWebRequest.CreateHttp(serverUrl + "/_replicate");
            request.Method = "POST";

            JObject json = new JObject();

            String sourceId = (isPull ? remote["url"] : local).ToString();
            String targetId = (isPull ? local : remote["url"]).ToString();

            json["source"] = isPull ? remote : local;
            json["target"] = isPull ? local : remote;
            json["create_target"] = createTarget;
            json["continuous"] = false;
            json["cancel"] = true;
            
            JObject response = execute(request, json);
            c8o.Log(C8oLogLevel.WARN, response.ToString());

            if (cancel)
            {
                return response;
            }

            json["cancel"] = false;

            request = HttpWebRequest.CreateHttp(serverUrl + "/_replicate");
            request.Method = "POST";

            response = null;

            JObject progress = new JObject();
            progress["direction"] = isPull ? "pull" : "push";
            progress["ok"] = true;
            progress["status"] = "Active";

            Task.Run(async () =>
            {
                long checkPoint_Interval = 1000;

                while (response == null)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(checkPoint_Interval));
                    
                    if (response != null)
                    {
                        break;
                    }

                    HttpWebRequest req = HttpWebRequest.CreateHttp(serverUrl + "/_active_tasks");
                    req.Method = "GET";

                    JObject res = execute(req);

                    if (response != null)
                    {
                        break;
                    }

                    c8o.Log(C8oLogLevel.WARN, res.ToString());

                    JObject task = null;
                    foreach (JToken item in res["item"])
                    {
                        if (item["target"].ToString() == targetId && item["source"].ToString() == sourceId)
                        {
                            task = item as JObject;
                            break;
                        }
                    }

                    if (task != null)
                    {
                        checkPoint_Interval = (long) task["checkpoint_interval"].ToObject(typeof(long));

                        progress["total"] = task["source_seq"];
                        progress["current"] = task["revisions_checked"];
                        progress["taskInfo"] = task.ToString();

                        HandleFullSyncResponse(progress, parameters, c8oResponseListener);

                        c8o.Log(C8oLogLevel.WARN, progress.ToString());
                    }
                }
            });

            response = execute(request, json);
            response.Remove("_c8oMeta");

            progress["total"] = response["source_last_seq"];
            progress["current"] = response["source_last_seq"];
            progress["taskInfo"] = response.ToString();
            progress["status"] = "Stopped";
            
            if (continuous)
            {

                progress["status"] = "Idle";
                json["continuous"] = true;

                request = HttpWebRequest.CreateHttp(serverUrl + "/_replicate");
                request.Method = "POST";

                response = execute(request, json);
                c8o.Log(C8oLogLevel.WARN, response.ToString());
                /*
                String localId = response["_local_id"].ToString();
                localId = localId.Substring(0, localId.IndexOf('+'));

                for (int i = 0; i < 1000; i++)
                {
                    request = HttpWebRequest.CreateHttp(getDatabaseUrl(fullSyncDatatbaseName) + "/_local/" + localId);
                    c8o.Log(C8oLogLevel.WARN, request.RequestUri.ToString());
                    request.Method = "GET";

                    response = execute(request);
                    c8o.Log(C8oLogLevel.WARN, response.ToString());
                }
                */
            }
            else
            {
                progress["status"] = "Stopped";
            }

            HandleFullSyncResponse(progress, parameters, c8oResponseListener);

            return VoidResponse.GetInstance();
        }

        public override Object HandleResetDatabaseRequest(String fullSyncDatatbaseName)
        {
            String uri = getDatabaseUrl(fullSyncDatatbaseName);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "DELETE";

            execute(request);

            request = HttpWebRequest.CreateHttp(uri);
            request.Method = "PUT";

            return execute(request);
        }

        public override LocalCacheResponse GetResponseFromLocalCache(String c8oCallRequestIdentifier)
        {
            return null;
        }

        //public override Object GetResponseFromLocalCache(String c8oCallRequestIdentifier)
        //{
        //    JObject localCacheDocument = HandleGetDocumentRequest(C8o.LOCAL_CACHE_DATABASE_NAME, c8oCallRequestIdentifier) as JObject;

        //    if (localCacheDocument == null)
        //    {
        //        throw new C8oUnavailableLocalCacheException(C8oExceptionMessage.ToDo());
        //    }


        //    String responseString = "" + localCacheDocument[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE];
        //    String responseTypeString = "" + localCacheDocument[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE];
        //    Object expirationDate = localCacheDocument[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE];

        //    long expirationDateLong;

        //    if (expirationDate != null)
        //    {
        //        if (expirationDate is long)
        //        {
        //            expirationDateLong = (long) expirationDate;
        //            double currentTime = C8oUtils.GetUnixEpochTime(DateTime.Now);
        //            if (expirationDateLong < currentTime)
        //            {
        //                throw new C8oUnavailableLocalCacheException(C8oExceptionMessage.timeToLiveExpired());
        //            }
        //        }
        //        else
        //        {
        //            throw new C8oUnavailableLocalCacheException(C8oExceptionMessage.invalidLocalCacheResponseInformation());
        //        }
        //    }

        //    if (responseTypeString.Equals(C8o.RESPONSE_TYPE_JSON))
        //    {
        //        return C8oTranslator.StringToJson(responseString);
        //    }
        //    else if (responseTypeString.Equals(C8o.RESPONSE_TYPE_XML))
        //    {
        //        return C8oTranslator.StringToXml(responseString);
        //    }
        //    else
        //    {
        //        throw new C8oException(C8oExceptionMessage.ToDo());
        //    }
        //}

        public override void SaveResponseToLocalCache(String c8oCallRequestIdentifier, LocalCacheResponse localCacheResponse)
        {
        }

        //public override void SaveResponseToLocalCache(String c8oCallRequestIdentifier, String responseString, String responseType, int timeToLive)
        //{
        //    Dictionary<String, Object> properties = new Dictionary<String, Object>();
        //    properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE] = responseString;
        //    properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE] = responseType;

        //    if (timeToLive != null)
        //    {
        //        long expirationDate = (long) C8oUtils.GetUnixEpochTime(DateTime.Now) + timeToLive;
        //        properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE] = expirationDate;
        //    }

        //    handlePostDocumentRequest(C8o.LOCAL_CACHE_DATABASE_NAME, FullSyncPolicy.OVERRIDE, properties);
        //}

        private Dictionary<String, Object> handleRev(String fullSyncDatatbaseName, String docid, Dictionary<String, Object> parameters)
        {
            String rev = C8oUtils.GetParameterStringValue(parameters, FullSyncDeleteDocumentParameter.REV.name, false);
            if (rev == null)
            {
                rev = getDocumentRev(fullSyncDatatbaseName, docid);
                if (rev != null)
                {
                    parameters[FullSyncDeleteDocumentParameter.REV.name] = getDocumentRev(fullSyncDatatbaseName, docid);
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
                JObject _c8oMeta = head["_c8oMeta"] as JObject;
                if ("success" == _c8oMeta["status"].ToString())
                {
                    rev = (_c8oMeta["headers"] as JObject)["ETag"].ToString();
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
                        json["item"] = JArray.Parse(entityContent);
                    }
                    catch (Exception e2)
                    {
                        json["data"] = entityContent;
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
                    json["data"] = entityContent;
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
			c8oMeta["statusCode"] = code;
			
			String status =
					code < 100 ? "unknown" :
					code < 200 ? "informational" :
					code < 300 ? "success" :
					code < 400 ? "redirection" :
					code < 500 ? "client error" :
					code < 600 ? "server error" : "unknown";
			c8oMeta["status"] = status;
			
			c8oMeta["reasonPhrase"] = response.StatusDescription;
			
			JObject headers = new JObject();

            foreach (String name in response.Headers.AllKeys)
            {
                headers[name] = response.Headers[name];
            }

            c8oMeta["headers"] = headers;
			
			json["_c8oMeta"] = c8oMeta;

            response.Dispose();

            return json;
        }
    }
}
