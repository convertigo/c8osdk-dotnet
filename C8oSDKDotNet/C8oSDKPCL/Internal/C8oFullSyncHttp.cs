using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Convertigo.SDK.Internal
{
    internal class C8oFullSyncHttp : C8oFullSync
    {
        private static readonly Regex RE_CONTENT_TYPE = new Regex("(.*?)\\s*;\\s*charset=(.*?)\\s*", RegexOptions.IgnoreCase);
        private static readonly Regex RE_FS_USE = new Regex("^(?:_use_(.*)$|__)");

        private IDictionary<string, bool> databases = new Dictionary<string, bool>();
        private string serverUrl;
        private string authBasicHeader;

        public C8oFullSyncHttp(string serverUrl, string username = null, string password = null)
        {
            this.serverUrl = serverUrl;

            if (username != null && !string.IsNullOrWhiteSpace(username) && password != null && !string.IsNullOrWhiteSpace(password))
            {
                authBasicHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            }
        }

        public override void Init(C8o c8o)
        {
            base.Init(c8o);
        }

        private async Task CheckDatabase(string db)
        {
            if (!databases.ContainsKey(db))
            {
                await HandleCreateDatabaseRequest(db);
                databases[db] = true;
            }
        }

        public override object HandleFullSyncResponse(object response, C8oResponseListener listener)
        {
            return base.HandleFullSyncResponse(response, listener);
        }

        public async override Task<object> HandleGetDocumentRequest(string fullSyncDatatbaseName, string docid, IDictionary<string, object> parameters = null)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            string uri = HandleQuery(GetDocumentUrl(fullSyncDatatbaseName, docid), parameters);

            var request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";

            var document = await Execute(request);
            var attachmentsProperty = document[FULL_SYNC__ATTACHMENTS] as JObject;
            
            if (attachmentsProperty != null)
            {
                foreach (KeyValuePair<string, JToken> iAttachment in attachmentsProperty)
                {
                    var attachment = iAttachment.Value as JObject;
                    attachment["content_url"] = GetDocumentAttachmentUrl(fullSyncDatatbaseName, docid, iAttachment.Key);
                }
            }

            return document;
        }

        public async Task<object> HandleGetDocumentAttachment(string fullSyncDatatbaseName, string docidParameterValue, string attachmentName)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            string uri = GetDocumentUrl(fullSyncDatatbaseName, docidParameterValue) + "/" + attachmentName;

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";
            request.Accept = "application/octet-stream";

            return await Execute(request);
        }

        public async override Task<object> HandleDeleteDocumentRequest(string fullSyncDatatbaseName, string docid, IDictionary<string, object> parameters)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            parameters = await HandleRev(fullSyncDatatbaseName, docid, parameters);

            string uri = HandleQuery(GetDocumentUrl(fullSyncDatatbaseName, docid), parameters);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "DELETE";

            return await Execute(request);
        }

        public async override Task<object> HandlePostDocumentRequest(string fullSyncDatatbaseName, FullSyncPolicy fullSyncPolicy, IDictionary<string, object> parameters)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            var options = new Dictionary<string, object>();

            foreach (var parameter in parameters)
            {
                var isUse = RE_FS_USE.Match(parameter.Key);
                if (isUse.Success)
                {
                    if (isUse.Groups[1].Success)
                    {
                        options[isUse.Groups[1].Value] = parameter.Value;
                    }
                    parameters.Remove(parameter.Key);
                }
            }

            string uri = HandleQuery(GetDatabaseUrl(fullSyncDatatbaseName), options);

            var request = HttpWebRequest.CreateHttp(uri);
            request.Method = "POST";

            // Gets the subkey separator parameter
            string subkeySeparatorParameterValue = C8oUtils.PeekParameterStringValue(parameters, FullSyncPostDocumentParameter.SUBKEY_SEPARATOR.name, false);

            if (subkeySeparatorParameterValue == null)
            {
                subkeySeparatorParameterValue = ".";
            }

            var postData = new JObject();

            foreach (KeyValuePair<string, object> kvp in parameters)
            {
                var obj = postData;
                string key = kvp.Key;
                String[] paths = key.Split(subkeySeparatorParameterValue.ToCharArray());

                if (paths.Length > 1)
                {

                    for (int i = 0; i < paths.Length - 1; i++)
                    {
                        string path = paths[i];
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

            postData = await ApplyPolicy(fullSyncDatatbaseName, postData, fullSyncPolicy);
            
            return await Execute(request, postData);
        }

        public async override Task<object> HandlePutAttachmentRequest(string fullSyncDatatbaseName, string docid, string attachmentName, string attachmentContentType, Stream attachmentContent)
        {
            throw new NotImplementedException();
        }

        public override Task<object> HandleDeleteAttachmentRequest(string fullSyncDatatbaseName, string docid, string attachmentName)
        {
            throw new NotImplementedException();
        }

        public async override Task<object> HandleAllDocumentsRequest(string fullSyncDatatbaseName, IDictionary<string, object> parameters)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            string uri = HandleQuery(GetDocumentUrl(fullSyncDatatbaseName, "_all_docs"), parameters);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";

            return await Execute(request);
        }

        public async override Task<object> HandleGetViewRequest(string fullSyncDatatbaseName, string ddoc, string view, IDictionary<string, object> parameters)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            string uri = HandleQuery(GetDocumentUrl(fullSyncDatatbaseName, "_design/" + ddoc) + "/_view/" + view, parameters);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";

            return await Execute(request);
        }

        public async override Task<object> HandleSyncRequest(string fullSyncDatatbaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            Task.Run(async () =>
            {
                await HandleReplicatePushRequest(fullSyncDatatbaseName, parameters, c8oResponseListener);
            }).GetAwaiter();
            return await HandleReplicatePullRequest(fullSyncDatatbaseName, parameters, c8oResponseListener);
        }

        public async override Task<object> HandleReplicatePullRequest(string fullSyncDatatbaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            return await postReplicate(fullSyncDatatbaseName, parameters, c8oResponseListener, true);
        }

        public async override Task<object> HandleReplicatePushRequest(string fullSyncDatatbaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            await CheckDatabase(fullSyncDatatbaseName);
            return await postReplicate(fullSyncDatatbaseName, parameters, c8oResponseListener, false);
        }

        private async Task<object> postReplicate(string fullSyncDatatbaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener, bool isPull)
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
                cancel = parameters["cancel"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            JToken local = fullSyncDatatbaseName + localSuffix;
            var remote = new JObject();

            remote["url"] = fullSyncDatabaseUrlBase + fullSyncDatatbaseName + '/';

            var cookies = c8o.CookieStore;

            if (cookies.Count > 0)
            {
                var headers = new JObject();
                var cookieHeader = new StringBuilder();

                foreach (Cookie cookie in cookies.GetCookies(new Uri(c8o.Endpoint)))
                {
                    cookieHeader.Append(cookie.Name).Append("=").Append(cookie.Value).Append("; ");
                }

                cookieHeader.Remove(cookieHeader.Length - 2, 2);

                headers["Cookie"] = cookieHeader.ToString();
                remote["headers"] = headers;
            }
            
            var request = HttpWebRequest.CreateHttp(serverUrl + "/_replicate");
            request.Method = "POST";

            var json = new JObject();

            string sourceId = (isPull ? remote["url"] : local).ToString();
            string targetId = (isPull ? local : remote["url"]).ToString();

            json["source"] = isPull ? remote : local;
            json["target"] = isPull ? local : remote;
            json["create_target"] = createTarget;
            json["continuous"] = false;
            json["cancel"] = true;
            
            var response = await Execute(request, json);
            c8o.Log._Warn("CANCEL REPLICATE:\n" + response.ToString());

            if (cancel)
            {
                return response;
            }

            json["cancel"] = false;

            request = HttpWebRequest.CreateHttp(serverUrl + "/_replicate");
            request.Method = "POST";

            response = null;

            var param = new Dictionary<string, object>(parameters);
            var progress = new C8oProgress();
            progress.Pull = isPull;
            progress.Status = "Active";
            progress.Finished = false;


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

                    var req = HttpWebRequest.CreateHttp(serverUrl + "/_active_tasks");
                    req.Method = "GET";

                    var res = await Execute(req);

                    if (response != null)
                    {
                        break;
                    }

                    c8o.Log._Warn(res.ToString());

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

                        progress.Raw = task;
                        progress.Total = task["source_seq"].Value<long>();
                        progress.Current = task["revisions_checked"].Value<long>();
                        progress.TaskInfo = task.ToString();

                        c8o.Log._Warn(progress.ToString());

                        if (progress.Changed)
                        {
                            var newProgress = progress;
                            progress = new C8oProgress(progress);

                            if (c8oResponseListener != null && c8oResponseListener is C8oResponseProgressListener)
                            {
                                param[C8o.ENGINE_PARAMETER_PROGRESS] = newProgress;
                                (c8oResponseListener as C8oResponseProgressListener).OnProgressResponse(progress, param);
                            }
                        }
                    }
                }
            }).GetAwaiter();

            response = await Execute(request, json);
            response.Remove("_c8oMeta");

            progress.Total = response["source_last_seq"].Value<long>();
            progress.Current = response["source_last_seq"].Value<long>();
            progress.TaskInfo = response.ToString();
            progress.Status = "Stopped";
            progress.Finished = true;

            if (c8oResponseListener != null && c8oResponseListener is C8oResponseProgressListener)
            {
                (c8oResponseListener as C8oResponseProgressListener).OnProgressResponse(progress, param);
            }

            if (continuous)
            {
                progress.Status = "Idle";
                json["continuous"] = true;

                request = HttpWebRequest.CreateHttp(serverUrl + "/_replicate");
                request.Method = "POST";

                response = await Execute(request, json);
                c8o.Log._Warn(response.ToString());
                
                /*
                string localId = response["_local_id"].ToString();
                localId = localId.Substring(0, localId.IndexOf('+'));

                do {
                    request = HttpWebRequest.CreateHttp(GetDatabaseUrl(fullSyncDatatbaseName) + "/_local/" + localId);
                    c8o.Log(C8oLogLevel.WARN, request.RequestUri.ToString());
                    request.Method = "GET";

                    response = await Execute(request);
                    c8o.Log(C8oLogLevel.WARN, response.ToString());
                } while(response["hystory"] != null);
                */
            }

            return VoidResponse.GetInstance();
        }

        public async override Task<object> HandleResetDatabaseRequest(string fullSyncDatatbaseName)
        {
            string uri = GetDatabaseUrl(fullSyncDatatbaseName);

            var request = HttpWebRequest.CreateHttp(uri);
            request.Method = "DELETE";

            databases.Remove(fullSyncDatatbaseName);
            await Execute(request);

            request = HttpWebRequest.CreateHttp(uri);
            request.Method = "PUT";

            var ret = await Execute(request);
            databases[fullSyncDatatbaseName] = true;
            return ret;
        }

        public async override Task<object> HandleCreateDatabaseRequest(string fullSyncDatatbaseName)
        {
            string uri = GetDatabaseUrl(fullSyncDatatbaseName);

            var request = HttpWebRequest.CreateHttp(uri);
            request.Method = "PUT";

            var ret = await Execute(request);
            databases[fullSyncDatatbaseName] = true;
            return ret;
        }

        public async override Task<object> HandleDestroyDatabaseRequest(string fullSyncDatatbaseName)
        {
            string uri = GetDatabaseUrl(fullSyncDatatbaseName);

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "DELETE";

            databases.Remove(fullSyncDatatbaseName);
            return await Execute(request);
        }

        public override Task<C8oLocalCacheResponse> GetResponseFromLocalCache(string c8oCallRequestIdentifier)
        {
            return null;
        }

        //public override Object GetResponseFromLocalCache(string c8oCallRequestIdentifier)
        //{
        //    Dictionary<string, object> localCacheDocument = HandleGetDocumentRequest(C8o.LOCAL_CACHE_DATABASE_NAME, c8oCallRequestIdentifier) as Dictionary<string, object>;

        //    if (localCacheDocument == null)
        //    {
        //        throw new C8oUnavailableLocalCacheException(C8oExceptionMessage.ToDo());
        //    }


        //    string responsestring = "" + localCacheDocument[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE];
        //    string responseTypestring = "" + localCacheDocument[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE];
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

        public override /*async*/ Task SaveResponseToLocalCache(string c8oCallRequestIdentifier, C8oLocalCacheResponse localCacheResponse)
        {
            return null;
        }

        //public override void SaveResponseToLocalCache(string c8oCallRequestIdentifier, string responseString, string responseType, int localCacheTimeToLive)
        //{
        //    Dictionary<string, object> properties = new Dictionary<string, object>();
        //    properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE] = responseString;
        //    properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE] = responseType;

        //    if (localCacheTimeToLive != null)
        //    {
        //        long expirationDate = (long) C8oUtils.GetUnixEpochTime(DateTime.Now) + localCacheTimeToLive;
        //        properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE] = expirationDate;
        //    }

        //    handlePostDocumentRequest(C8o.LOCAL_CACHE_DATABASE_NAME, FullSyncPolicy.OVERRIDE, properties);
        //}

        private async Task<IDictionary<string, object>> HandleRev(string fullSyncDatatbaseName, string docid, IDictionary<string, object> parameters)
        {
            var parameter = C8oUtils.GetParameter(parameters, FullSyncDeleteDocumentParameter.REV.name, false);
            if (parameter.Key == null)
            {
                string rev = await GetDocumentRev(fullSyncDatatbaseName, docid);
                if (rev != null)
                {
                    parameters[FullSyncDeleteDocumentParameter.REV.name] = rev;
                }
            }
            return parameters;
        }

        private async Task<string> GetDocumentRev(string fullSyncDatatbaseName, string docid)
        {
            var head = await HeadDocument(fullSyncDatatbaseName, docid);
            string rev = null;
            try
            {
                var _c8oMeta = head["_c8oMeta"] as JObject;
                if ("success" == _c8oMeta["status"].ToString())
                {
                    rev = (_c8oMeta["headers"] as JObject)["ETag"].ToString();
                    rev = rev.Substring(1, rev.Length - 2);
                }
            }
            catch (Exception e)
            {
                c8o.Log._Debug("Cannot find revision of docid=" + docid, e);
            }

            return rev;
        }

        private async Task<JObject> HeadDocument(string fullSyncDatatbaseName, string docid)
        {
            string uri = GetDocumentUrl(fullSyncDatatbaseName, docid);

            var request = HttpWebRequest.CreateHttp(uri);
            request.Method = "HEAD";

            return await Execute(request);
        }

        private string GetDatabaseUrl(string db)
        {
		    if (String.IsNullOrWhiteSpace(db))
            {
                throw new ArgumentException("blank 'db' not allowed");
		    }

            db = WebUtility.UrlEncode(db);

		    return serverUrl + '/' + db + localSuffix;
	    }
	
	    private string GetDocumentUrl(string db, string docid)
        {
            if (String.IsNullOrWhiteSpace(docid))
            {
                throw new ArgumentException("blank 'docid' not allowed");
		    }

		    if (!docid.StartsWith("_design/")) {
                docid = WebUtility.UrlEncode(docid);
		    }

		    return GetDatabaseUrl(db) + '/' + docid;
	    }

        private string GetDocumentAttachmentUrl(string db, string docid, string attName)
        {
            if (String.IsNullOrWhiteSpace(attName))
            {
                throw new ArgumentException("blank 'docid' not allowed");
            }

            return GetDocumentUrl(db, docid) + '/' + attName;
        }

        private string HandleQuery(string url, IDictionary<string, object> query)
        {
		    StringBuilder uri = new StringBuilder(url);
		    if (query != null && query.Count > 0)
            {
                uri.Append("?");
                foreach (KeyValuePair<string, object> kvp in query)
                {
                    uri.Append(WebUtility.UrlEncode(kvp.Key)).Append("=").Append(WebUtility.UrlEncode(kvp.Value.ToString())).Append("&");
                }
                uri.Remove(uri.Length - 1, 1);
		    }
		    return uri.ToString();
        }
        private async Task<JObject> ApplyPolicy(string fullSyncDatatbaseName, JObject document, FullSyncPolicy fullSyncPolicy)
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
                string docid = document["_id"].ToString();

                if (docid != null)
                {
                    if (fullSyncPolicy == FullSyncPolicy.OVERRIDE)
                    {
                        string rev = await GetDocumentRev(fullSyncDatatbaseName, docid);

                        if (rev != null)
                        {
                            document["_rev"] = rev;
                        }
                    }
                    else if (fullSyncPolicy == FullSyncPolicy.MERGE)
                    {
                        var dbDocument = await HandleGetDocumentRequest(fullSyncDatatbaseName, docid) as JObject;

                        if (dbDocument["_id"] != null)
                        {
                            document.Remove("_rev");
                            Merge(dbDocument, document);
                            document = dbDocument;
                        }
                    }
                }
            }

            document.Remove("_c8oMeta");

            return document;
        }

        private void Merge(JObject jsonTarget, JObject jsonSource)
        {
            foreach (var kvp in jsonSource)
            {
                try
                {
                    var targetValue = jsonTarget[kvp.Key];
                    if (targetValue != null)
                    {
                        if (targetValue is JObject && kvp.Value is JObject)
                        {
                            Merge(targetValue as JObject, kvp.Value as JObject);
                        }
                        else if (targetValue is JArray && kvp.Value is JArray)
                        {
                            Merge(targetValue as JArray, kvp.Value as JArray);
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
                    c8o.Log._Info("Failed to merge json documents", e);
                }
            }
        }

        private void Merge(JArray targetArray, JArray sourceArray)
        {
            int targetSize = targetArray.Count;
            int sourceSize = sourceArray.Count;

            for (int i = 0; i < sourceSize; i++)
            {
                try
                {
                    JToken targetValue = targetSize > i ? targetArray[i] : null;
                    JToken sourceValue = sourceArray[i];
                    if (sourceValue != null && targetValue != null)
                    {
                        if (targetValue is JObject && sourceValue is JObject)
                        {
                            Merge(targetValue as JObject, sourceValue as JObject);
                        }
                        if (targetValue is JArray && sourceValue is JArray)
                        {
                            Merge(targetValue as JArray, sourceValue as JArray);
                        }
                        else
                        {
                            targetArray[i] = sourceValue;
                        }
                    }
                    else if (sourceValue != null && targetValue == null)
                    {
                        targetArray.Add(sourceValue);
                    }
                }
                catch (Exception e)
                {
                    c8o.Log._Info("Failed to merge json arrays", e);
                }
            }
        }

        private async Task<JObject> Execute(HttpWebRequest request, JObject document = null)
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

                using (var postStream = Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, request).Result)
                {

                    // postData = "__connector=HTTP_connector&__transaction=transac1&testVariable=TEST 01";
                    byte[] byteArray = Encoding.UTF8.GetBytes(document.ToString());
                    // Add the post data to the web request

                    postStream.Write(byteArray, 0, byteArray.Length);
                }
            }

            HttpWebResponse response;
            try
            {
                response = await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request) as HttpWebResponse;
            }
            catch (WebException e)
            {
                response = e.Response as HttpWebResponse;
                if (response == null)
                {
                    throw new C8oHttpException(C8oExceptionMessage.RunHttpRequest(), e);
                }
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

            var matchContentType = RE_CONTENT_TYPE.Match(response.ContentType);

            string contentType;
            string charset;
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
                string entityContent = streamReader.ReadToEnd();
                try
                {
                    json = JObject.Parse(entityContent);
                }
                catch
                {
                    json = new JObject();
                    try
                    {
                        json["item"] = JArray.Parse(entityContent);
                    }
                    catch
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
                    string entityContent = streamReader.ReadToEnd();
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

            var c8oMeta = new JObject();
			
			int code = (int) response.StatusCode;
			c8oMeta["statusCode"] = code;
			
			string status =
					code < 100 ? "unknown" :
					code < 200 ? "informational" :
					code < 300 ? "success" :
					code < 400 ? "redirection" :
					code < 500 ? "client error" :
					code < 600 ? "server error" : "unknown";
			c8oMeta["status"] = status;
			
			c8oMeta["reasonPhrase"] = response.StatusDescription;

            var headers = new JObject();

            foreach (string name in response.Headers.AllKeys)
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
