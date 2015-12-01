using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Convertigo.SDK.FullSync;
using Convertigo.SDK;
using Couchbase.Lite;
using Convertigo.SDK.Utils;
using Convertigo.SDK.FullSync.Enums;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Convertigo.SDK.Exceptions;
using Newtonsoft.Json;
using System.Reflection;
using System.IO;
using Convertigo.SDK.FullSync.Responses;
using Convertigo.SDK.C8oEnum;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    internal class C8oFullSyncCbl : C8oFullSync
    {
        //*** Constants ***//

        private static readonly string JS_VIEW_COMPILER_TYPE = "Couchbase.Lite.Listener.JSViewCompiler";

        private static readonly string ATTACHMENT_INTERNAL_TYPE = "Couchbase.Lite.Internal.AttachmentInternal";
        private static readonly string ATTACHMENT_INTERNAL_PROPERTY_DATABASE = "Database";
        private static readonly string ATTACHMENT_INTERNAL_PROPERTY_CONTENT_URL = "ContentUrl";

        private static readonly string ATTACHMENT_PROPERTY_KEY_CONTENT_URL = "content_url";

        //*** Attributes ***//

        /// <summary>
        /// Manages a collection of CBL Database instances.
        /// </summary>
        private Manager manager;
        private IDictionary<string, C8oFullSyncDatabase> fullSyncDatabases;

        //*** Constructors / Initializations ***//

        public C8oFullSyncCbl()
        {
        }

        public override void Init(C8o c8o)
        {
            base.Init(c8o);

            fullSyncDatabases = new Dictionary<string, C8oFullSyncDatabase>();
            manager = Manager.SharedInstance;
        }

        public Task<C8oFullSyncDatabase> GetOrCreateFullSyncDatabase(string databaseName)
        {
            string localDatabaseName = databaseName + localSuffix;
            if (!fullSyncDatabases.ContainsKey(localDatabaseName))
            {
                fullSyncDatabases[localDatabaseName] = new C8oFullSyncDatabase(c8o, manager, databaseName, fullSyncDatabaseUrlBase, localSuffix);
            }
            return Task.FromResult<C8oFullSyncDatabase>(fullSyncDatabases[localDatabaseName]);
        }

        //*** Request handlers ***//

        public override object HandleFullSyncResponse(object response, C8oResponseListener listener)
        {
            response = base.HandleFullSyncResponse(response, listener);
            if (response is VoidResponse)
            {
                return response;
            }

            if (listener is C8oResponseJsonListener)
            {
                //*** Document (GetDocument) ***//
                if (response is Document)
                {
                    return C8oFullSyncCblTranslator.DocumentToJson(response as Document);
                }
                //*** FullSyncDocumentOperationResponse (DeleteDocument, PostDocument) ***//
                else if (response is FullSyncDocumentOperationResponse)
                {
                    return C8oFullSyncCblTranslator.FullSyncDocumentOperationResponseToJSON(response as FullSyncDocumentOperationResponse);
                }
                //*** QueryEnumerator (GetAllDocuments, GetView) ***//
                else if (response is QueryEnumerator)
                {
                    try
                    {
                        return C8oFullSyncCblTranslator.QueryEnumeratorToJson(response as QueryEnumerator);
                    }
                    catch (C8oException e)
                    {
                        throw new C8oException(C8oExceptionMessage.queryEnumeratorToJSON(), e);
                    }
                }
                //*** FullSyncDefaultResponse (Sync, ReplicatePull, ReplicatePush, Reset) ***//
                else if (response is FullSyncDefaultResponse)
                {
                    return C8oFullSyncCblTranslator.FullSyncDefaultResponseToJson(response as FullSyncDefaultResponse);
                }
            }
            else if (listener is C8oResponseXmlListener)
            {
                //*** Document (GetDocument) ***//
                if (response is Document)
                {
                    return C8oFullSyncCblTranslator.DocumentToXml(response as Document);
                }
                //*** FullSyncDocumentOperationResponse (DeleteDocument, PostDocument) ***//
                else if (response is FullSyncDocumentOperationResponse)
                {
                    return C8oFullSyncCblTranslator.FullSyncDocumentOperationResponseToXml(response as FullSyncDocumentOperationResponse);
                }
                //*** QueryEnumerator (GetAllDocuments, GetView) ***//
                else if (response is QueryEnumerator)
                {
                    try
                    {
                        return C8oFullSyncCblTranslator.QueryEnumeratorToXml(response as QueryEnumerator);
                    }
                    catch (C8oException e)
                    {
                        throw new C8oException(C8oExceptionMessage.queryEnumeratorToXML(), e);
                    }
                }
                //*** FullSyncDefaultResponse (Sync, ReplicatePull, ReplicatePush, Reset) ***//
                else if (response is FullSyncDefaultResponse)
                {
                    return C8oFullSyncCblTranslator.FullSyncDefaultResponseToXml(response as FullSyncDefaultResponse);
                }
            }
            else if (listener is C8oResponseCblListener)
            {
                //*** Document (GetDocument) ***// || //*** QueryEnumerator (GetAllDocuments, GetView) ***//
                if (response is Document || response is QueryEnumerator)
                {
                    return response;
                }
            }
            return response;
        }

        //*** GetDocument ***//

        /// <summary>
        /// Returns the requested document.
        /// </summary>
        /// <param name="fullSyncDatatbase"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async override Task<object> HandleGetDocumentRequest(string fullSyncDatatbaseName, string docid, IDictionary<string, object> parameters)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(fullSyncDatatbaseName);

            // Gets the document form the local database
            var document = fullSyncDatabase.Database.GetExistingDocument(docid);            

            if (document != null)
            {
                // If there are attachments, compute for each one the url to local storage and add it to the attachment descriptor
                var attachmentsProperty = document.GetProperty(C8oFullSync.FULL_SYNC__ATTACHMENTS) as JObject;
                if (attachmentsProperty != null)
                {
                    SavedRevision rev = document.CurrentRevision;
                    Assembly couchbaseLiteAssembly = Assembly.GetAssembly(typeof(Attachment));
                    Type attachmentInternalType = couchbaseLiteAssembly.GetType(ATTACHMENT_INTERNAL_TYPE);
                    ConstructorInfo attachmentInternalConstructor = attachmentInternalType.GetConstructor(new Type[] { typeof(String), typeof(IDictionary<string, object>) });

                    foreach (var attachmentProperty in attachmentsProperty)
                    {
                        string attachmentName = attachmentProperty.Key;
                        Attachment attachment = rev.GetAttachment(attachmentName);
                        if (!attachment.Metadata.Keys.Contains(ATTACHMENT_PROPERTY_KEY_CONTENT_URL))
                        {
                            Object[] attachmentInternalConstructorParams = new Object[] { attachment.Name, attachment.Metadata };
                            object attachmentInternal = attachmentInternalConstructor.Invoke(attachmentInternalConstructorParams);

                            PropertyInfo databaseProp = attachmentInternalType.GetProperty(ATTACHMENT_INTERNAL_PROPERTY_DATABASE);
                            databaseProp.SetValue(attachmentInternal, fullSyncDatabase.Database);

                            PropertyInfo urlProp = attachmentInternalType.GetProperty(ATTACHMENT_INTERNAL_PROPERTY_CONTENT_URL);
                            object contentUrl = urlProp.GetValue(attachmentInternal, null);
                            if (contentUrl != null && contentUrl is Uri)
                            {
                                Uri uri = (Uri)contentUrl;
                                string absoluteUri = C8oUtils.UrlDecode(uri.AbsoluteUri);
                                string absolutePath = C8oUtils.UrlDecode(uri.AbsolutePath);
                                attachment.Metadata.Add(ATTACHMENT_PROPERTY_KEY_CONTENT_URL, absoluteUri);
                                if (attachmentProperty.Value is JObject)
                                {
                                    (attachmentProperty.Value as JObject)[ATTACHMENT_PROPERTY_KEY_CONTENT_URL] = absoluteUri;
                                }
                                attachment.Metadata.Add("content_path", absolutePath);
                                if (attachmentProperty.Value is JObject)
                                {
                                    (attachmentProperty.Value as JObject)["content_path"] = absolutePath;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new C8oRessourceNotFoundException(C8oExceptionMessage.RessourceNotFound("requested document"));
            }

            return document;
        }

        //*** DeleteDocument ***//

        /// <summary>
        /// Deletes an existing document from the local database.
        /// </summary>
        /// <param name="fullSyncDatabase"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async override Task<object> HandleDeleteDocumentRequest(string fullSyncDatatbaseName, string docid, IDictionary<string, object> parameters)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(fullSyncDatatbaseName);

            string revParameterValue = C8oUtils.GetParameterStringValue(parameters, FullSyncDeleteDocumentParameter.REV.name, false);

            var document = fullSyncDatabase.Database.GetExistingDocument(docid);
            if (document == null)
            {
                throw new C8oRessourceNotFoundException(C8oExceptionMessage.RessourceNotFound("requested document"));
            }

            string documentRevision = document.CurrentRevisionId;

            // If the revision is specified then checks if this is the right revision
            if (revParameterValue != null && !revParameterValue.Equals(documentRevision))
            {
                throw new C8oRessourceNotFoundException(C8oExceptionMessage.RessourceNotFound("requested document"));
            }

            try
            {
                document.Delete();
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }
            bool deleted = document.Deleted;

            return new FullSyncDocumentOperationResponse(docid, revParameterValue, deleted);
        }

        //*** PostDocument ***//

        public async override Task<object> HandlePostDocumentRequest(string databaseName, FullSyncPolicy fullSyncPolicy, IDictionary<string, object> parameters)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(databaseName);
            
            // Gets the subkey separator parameter
            string subkeySeparatorParameterValue = C8oUtils.GetParameterStringValue(parameters, FullSyncPostDocumentParameter.SUBKEY_SEPARATOR.name, false);
            if (subkeySeparatorParameterValue == null)
            {
                subkeySeparatorParameterValue = ".";
            }

            // Filters and modifies wrong properties
		    var newProperties = new Dictionary<string, object>();
		    foreach (var parameter in parameters) {
			    string parameterName = parameter.Key;

                // Ignores parameters beginning with "__" or "_use_" 
                if (parameterName.StartsWith("__"))
                {
				    continue;
			    }
                if (parameterName.StartsWith("_use_"))
                {
				    continue;
			    }
			
                // Retrieves ???
			    var objectParameterValue = C8oUtils.GetParameterJsonValue(parameter);

			    // Checks if the parameter name is splittable
                var paths = parameterName.Split(new String[] { subkeySeparatorParameterValue }, StringSplitOptions.None); // Regex.Split(parameterName, subkeySeparatorParameterValue);
			    if (paths.Length > 1) {
                    // The first substring becomes the key
                    parameterName = paths[0];
                    // Next substrings create a hierarchy which will becomes json subkeys  
				    int count = paths.Length - 1;
				    while (count > 0) {
                        var tmpObject = new Dictionary<string, object>();
					    tmpObject[paths[count]] = objectParameterValue;
					    objectParameterValue = tmpObject;
					    count--;
				    }
			    }

			    newProperties[parameterName] = objectParameterValue;
		    }

            var createdDocument = C8oFullSyncCblEnum.PostDocument(fullSyncPolicy, fullSyncDatabase.Database, newProperties);
            // Document createdDocument = fullSyncPolicy.PostDocument(fullSyncDatabase, newProperties);
            string documentId = createdDocument.Id;
            string currentRevision = createdDocument.CurrentRevisionId;
            return new FullSyncDocumentOperationResponse(documentId, currentRevision, true);
        }

        //*** GetAllDocuments ***//

        public async override Task<object> HandleAllDocumentsRequest(string databaseName, IDictionary<string, object> parameters)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(databaseName);

            /*
            // !!! TMP !!!
            if (parameters.ContainsKey("count"))
            {
                IDictionary<string, object> json = new Dictionary<string, object>();
                json.Add("count", database.DocumentCount);
                return json;
            }
            // !!! TMP !!!
            */

            // Creates the fullSync query and add parameters to it
            var query = fullSyncDatabase.Database.CreateAllDocumentsQuery();
            
            AddParametersToQuery(query, parameters);

            var result = query.Run();
            /*
            if (parameters["include_docs"] == (true as Object))
            {
                IEnumerator<QueryRow> queryRows = queryEnumerator.GetEnumerator();
                while (queryRows.MoveNext())
                {
                    QueryRow queryRow = queryRows.Current;
                    IDictionary<string, object> queryRowJson = FullSyncTranslator.DictionaryToJson(queryRow.AsJSONDictionary());
                    rowsArray.Add(queryRowJson);
                }
            }
            */

            var obj = C8oFullSyncCblTranslator.QueryEnumeratorToJson(result);
            obj["total_rows"] = fullSyncDatabase.Database.DocumentCount;


            return obj;
        }

        //*** GetView ***//

        public async override Task<object> HandleGetViewRequest(string databaseName, string ddocName, string viewName, IDictionary<string, object> parameters)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(databaseName);

            // Gets the view depending to its programming language (Javascript / C#)
            Couchbase.Lite.View view;
            if (ddocName != null)
            {
                // Javascript view
                view = CheckAndCreateJavaScriptView(fullSyncDatabase.Database, ddocName, viewName);
            }
            else
            {
                // C# view
                view = fullSyncDatabase.Database.GetView(viewName);
            }
            if (view == null)
            {
                // Error
            }

            // Creates the fullSync query and add parameters to it
            var query = view.CreateQuery();
            AddParametersToQuery(query, parameters);

            var result = query.Run();
            return result;
        }

        //*** Sync, ReplicatePull, ReplicatePush ***//

        public async override Task<object> HandleSyncRequest(string databaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(databaseName);
            try
            {
                fullSyncDatabase.StartAllReplications(parameters, c8oResponseListener);
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }
            return VoidResponse.GetInstance();
        }

        public async override Task<object> HandleReplicatePullRequest(string databaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            try
            {
                var fullSyncDatabase = await GetOrCreateFullSyncDatabase(databaseName);
                fullSyncDatabase.StartPullReplication(parameters, c8oResponseListener);
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }
            return VoidResponse.GetInstance();
        }

        public async override Task<object> HandleReplicatePushRequest(string databaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(databaseName);
            try
            {
                fullSyncDatabase.StartPushReplication(parameters, c8oResponseListener);
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }
            return VoidResponse.GetInstance();
        }

        //*** Reset ***//

        public async override Task<object> HandleResetDatabaseRequest(string databaseName)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(databaseName);
            if (fullSyncDatabase != null)
            {
                fullSyncDatabase.Database.Delete();
                fullSyncDatabases.Remove(fullSyncDatabase.DatabaseName);
            }
		    return new FullSyncDefaultResponse(true);
	    }

        public async override Task<object> HandleCreateDatabaseRequest(string databaseName)
        {
            var fullSyncDatabase = await GetOrCreateFullSyncDatabase(databaseName);
            return new FullSyncDefaultResponse(true);
        }

        public async override Task<object> HandleDestroyDatabaseRequest(string databaseName)
        {
            var db = manager.GetDatabase(databaseName + localSuffix);
            if (db != null)
            {
                manager.ForgetDatabase(db);
            }
            return new FullSyncDefaultResponse(true);
        }

        //*** JavaScript View ***//

        private Couchbase.Lite.View CompileView(string viewName, JObject viewProps, Database database)
        {
            JToken language;
            if (!viewProps.TryGetValue("language", out language))
            {
                language = "javascript";
            }
            JToken mapSource;
            if (!viewProps.TryGetValue("map", out mapSource))
            {
                return null;
            }

            //Assembly couchbaseLiteAssembly = Assembly.GetAssembly(typeof(IViewCompiler));
            //string tmp = "";
            //foreach (Type type in couchbaseLiteAssembly.ExportedTypes)
            //{
            //    tmp = tmp + type.Namespace + " // " + type.Name + " \n";
            //}
            //Type jsViewCompilerType = couchbaseLiteAssembly.GetType(FullSyncMobile.JS_VIEW_COMPILER_TYPE);
            //ConstructorInfo[] constructors = jsViewCompilerType.GetConstructors();
            // ConstructorInfo attachmentInternalConstructor = attachmentInternalType.GetConstructor(new Type[] { typeof(String), typeof(IDictionary<string, object>) });
            // object attachmentInternal = attachmentInternalConstructor.Invoke(attachmentInternalConstructorParams);

            IViewCompiler viewCompiler = Couchbase.Lite.View.Compiler;
            IViewCompiler test = new JSViewCompilerCopy();
            Couchbase.Lite.View.Compiler = test;
            MapDelegate mapBlock = Couchbase.Lite.View.Compiler.CompileMap((String) mapSource, (String) language);
            if (mapBlock == null)
            {
                return null;
            }

            JToken reduceSource = null;
            ReduceDelegate reduceBlock = null;
            if (viewProps.TryGetValue("reduce", out reduceSource))
            {
                // Couchbase.Lite.View.compiler est null et Couchbase.Lite.Listener.JSViewCompiler est inaccessible (même avec la reflection)
                
                reduceBlock = Couchbase.Lite.View.Compiler.CompileReduce((String) reduceSource, (String) language);
                if (reduceBlock == null)
                {
                    return null;
                }
            }

            Couchbase.Lite.View view = database.GetView(viewName);
            view.SetMapReduce(mapBlock, reduceBlock, "1");
            JToken collation = null;
            if (viewProps.TryGetValue("collation", out collation))
            {
                if ("raw".Equals((String) collation)) 
                {
                    // ???
                    
                }
            }

            return view;
        }

        private Couchbase.Lite.View CheckAndCreateJavaScriptView(Database database, string ddocName, string viewName)
        {
            string tdViewName = ddocName + "/" + viewName;
            Couchbase.Lite.View view = database.GetExistingView(tdViewName);

            if (view == null || view.Map == null)
            {
                // No TouchDB view is defined, or it hasn't had a map block assigned
                // Searches in the design document if there is a CouchDB view definition we can compile

                Document designDocument = database.GetExistingDocument(C8oFullSync.FULL_SYNC_DDOC_PREFIX + "/" + ddocName);
                JObject views = designDocument.GetProperty(FULL_SYNC_VIEWS) as JObject;
                JToken viewProps;
                if (!views.TryGetValue(viewName, out viewProps))
                {
                    return null;
                }
                view = CompileView(viewName, viewProps as JObject, database);

                // ???
            }

            return view;
        }

        //*** Local cache ***//

        public override async Task<C8oLocalCacheResponse> GetResponseFromLocalCache(string c8oCallRequestIdentifier)
        {
            C8oFullSyncDatabase fullSyncDatabase = await GetOrCreateFullSyncDatabase(C8o.LOCAL_CACHE_DATABASE_NAME);
            Document localCacheDocument = fullSyncDatabase.Database.GetExistingDocument(c8oCallRequestIdentifier);

            if (localCacheDocument == null)
            {
                throw new C8oUnavailableLocalCacheException(C8oExceptionMessage.ToDo());
            }

            IDictionary<string, object> properties = localCacheDocument.Properties;

            string response;
            string responseType;
            long expirationDate;
            if (!C8oUtils.TryGetParameterObjectValue<String>(properties, C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE, out response) || 
                !C8oUtils.TryGetParameterObjectValue<String>(properties, C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE, out responseType))
            {
                throw new C8oUnavailableLocalCacheException(C8oExceptionMessage.invalidLocalCacheResponseInformation());
            }
            if (!C8oUtils.TryGetParameterObjectValue<long>(properties, C8o.LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE, out expirationDate))
            {
                expirationDate = -1;
            }

            return new C8oLocalCacheResponse(response, responseType, expirationDate);
        }


        public override async Task SaveResponseToLocalCache(string c8oCallRequestIdentifier, C8oLocalCacheResponse localCacheResponse)
        {
            C8oFullSyncDatabase fullSyncDatabase = await GetOrCreateFullSyncDatabase(C8o.LOCAL_CACHE_DATABASE_NAME);
            Document localCacheDocument = fullSyncDatabase.Database.GetDocument(c8oCallRequestIdentifier);

            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE] = localCacheResponse.Response;
            properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE] = localCacheResponse.ResponseType;
            if (localCacheResponse.ExpirationDate > 0)
            {
                properties[C8o.LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE] = localCacheResponse.ExpirationDate;
            }

            SavedRevision currentRevision = localCacheDocument.CurrentRevision;
            if (currentRevision != null)
            {
                properties[FULL_SYNC__REV] = currentRevision.Id;
            }

            localCacheDocument.PutProperties(properties);
        }

        //*** Other ***//
        
        /// <summary>
        /// Adds known parameters to the fullSync query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        private static void AddParametersToQuery(Query query, IDictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                FullSyncRequestParameter fullSyncRequestParameter = FullSyncRequestParameter.GetFullSyncRequestParameter(parameter.Key);
                // If the parameter corresponds to a FullSyncRequestParameter
                if (fullSyncRequestParameter != null)
                {
                    object parameterValue = parameter.Value;
                    //object objectParameterValue = parameterValue;
                    //if (parameterValue is String)
                    //{
                    //    // Passer par une fonction ??,,
                    //    objectParameterValue = JsonConvert.DeserializeObject(parameterValue as String, fullSyncRequestParameter.type);
                    //}
                    // fullSyncRequestParameter.AddToQuery(query, objectParameterValue);
                    C8oFullSyncCblEnum.AddToQuery(query, fullSyncRequestParameter, parameterValue);
                }
            }
        }

        //protected override FullSyncDatabase2 CreateFullSyncDatabase(string databaseName)
        //{
        //    return new FullSyncDatabase(manager, databaseName, fullSyncDatabaseUrlBase, c8o);
        //}

        public static void Init()
        {
            C8o.C8oFullSyncUsed = Type.GetType("Convertigo.SDK.C8oFullSyncCbl", true);
        }

    }

}