using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Convertigo.SDK.FullSync;
using Convertigo.SDK;
using Convertigo.SDK.Listeners;
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

namespace Convertigo.SDK.FullSync
{
    public class FullSyncMobile : FullSyncInterface
    {

        //*** Constants ***//

        private static readonly String JS_VIEW_COMPILER_TYPE = "Couchbase.Lite.Listener.JSViewCompiler";

        private static readonly String ATTACHMENT_INTERNAL_TYPE = "Couchbase.Lite.Internal.AttachmentInternal";
        private static readonly String ATTACHMENT_INTERNAL_PROPERTY_DATABASE = "Database";
        private static readonly String ATTACHMENT_INTERNAL_PROPERTY_CONTENT_URL = "ContentUrl";

        private static readonly String ATTACHMENT_PROPERTY_KEY_CONTENT_URL = "content_url";

        //*** Attributes ***//

        private C8o c8o;
        /// <summary>
        /// Manages a collection of CBL Database instances.
        /// </summary>
        private Manager manager;
        private String fullSyncDatabaseUrlBase;
        private List<CblDatabase> fullSyncDatabases;

        //*** Constructors / Initializations ***//

        public FullSyncMobile()
        {
        }

        public override void Init(C8o c8o, C8oSettings c8oSettings, String endpointFirstPart)
        {
            base.Init(c8o, c8oSettings, endpointFirstPart);

            this.fullSyncDatabases = new List<CblDatabase>();

            this.c8o = c8o;
            this.fullSyncDatabaseUrlBase = endpointFirstPart + FullSyncInterface.FULL_SYNC_URL_PATH;
            this.manager = Manager.SharedInstance;

            // If the default fullSync database name is specified then adds the related database to the list
            if (this.defaultFullSyncDatabaseName != null)
            {
                try
                {
                    // this.fullSyncDatabases.Add(new FullSyncDatabase(this.manager, this.defaultFullSyncDatabaseName, this.fullSyncDatabaseUrlBase, this.c8o));
                }
                catch (Exception e)
                {
                    throw new C8oException(C8oExceptionMessage.ToDo(), e);
                }
            }
        }

        //*** Request handlers ***//

        public override void HandleFullSyncResponse(Object response, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            if (c8oResponseListener == null)
            {
                return;
            }

            // Calls back the listener
            if (c8oResponseListener is C8oCblResponseListener)
            {
                if (response is Document)
                {
                    (c8oResponseListener as C8oCblResponseListener).OnDocumentResponse((response as Document), parameters);
                }
                else if (response is QueryEnumerator)
                {
                    (c8oResponseListener as C8oCblResponseListener).OnQueryEnumeratorResponse((response as QueryEnumerator), parameters);
                }
            }
            else if (c8oResponseListener is C8oJsonResponseListener)
            {
                //*** Document (GetDocument) ***//
			    if (response is Document) 
                {
				    (c8oResponseListener as C8oJsonResponseListener).OnJsonResponse(CblTranslator.DocumentToJson((Document) response), parameters);
			    } 
			    //*** FullSyncDocumentOperationResponse (DeleteDocument, PostDocument) ***//
			    else if (response is FullSyncDocumentOperationResponse) 
                {
				    (c8oResponseListener as C8oJsonResponseListener).OnJsonResponse(CblTranslator.FullSyncDocumentOperationResponseToJSON((FullSyncDocumentOperationResponse) response), parameters);
			    }
			    //*** QueryEnumerator (GetAllDocuments, GetView) ***// 
			    else if (response is QueryEnumerator) 
                {
					(c8oResponseListener as C8oJsonResponseListener).OnJsonResponse(CblTranslator.QueryEnumeratorToJson((QueryEnumerator) response), parameters);
			    } 
			    //*** FullSyncDefaultResponse (Sync, ReplicatePull, ReplicatePush, Reset) ***//
                else if (response is FullSyncDefaultResponse)
                {
                    (c8oResponseListener as C8oJsonResponseListener).OnJsonResponse(CblTranslator.FullSyncDefaultResponseToJson((FullSyncDefaultResponse)response), parameters);
                }
                // !!! TMP !!!
                else if (response is JObject)
                {
                    (c8oResponseListener as C8oJsonResponseListener).OnJsonResponse(response as JObject, parameters);
                }
            }
            else if (c8oResponseListener is C8oXmlResponseListener)
            {
                //*** Document (GetDocument) ***//
                if (response is Document)
                {
                    (c8oResponseListener as C8oXmlResponseListener).OnXmlResponse(CblTranslator.DocumentToXml((Document)response), parameters);
                }
                //*** FullSyncDocumentOperationResponse (DeleteDocument, PostDocument) ***//
                else if (response is FullSyncDocumentOperationResponse)
                {
                    (c8oResponseListener as C8oXmlResponseListener).OnXmlResponse(CblTranslator.FullSyncDocumentOperationResponseToXml((FullSyncDocumentOperationResponse)response), parameters);
                }
                //*** QueryEnumerator (GetAllDocuments, GetView) ***// 
                else if (response is QueryEnumerator)
                {
                    (c8oResponseListener as C8oXmlResponseListener).OnXmlResponse(CblTranslator.QueryEnumeratorToXml((QueryEnumerator)response), parameters);
                }
                //*** FullSyncDefaultResponse (Sync, ReplicatePull, ReplicatePush, Reset) ***//
                else if (response is FullSyncDefaultResponse)
                {
                    (c8oResponseListener as C8oXmlResponseListener).OnXmlResponse(CblTranslator.FullSyncDefaultResponseToXml((FullSyncDefaultResponse)response), parameters);
                }
            }
            else
            {
                throw new ArgumentException(C8oExceptionMessage.UnknownType("c8oResponseListener", c8oResponseListener));
            }
        }

        private CblDatabase FindDatabase(String databaseName)
        {
            CblDatabase fullSyncDatabase = this.fullSyncDatabases.Find(x => x.DatabaseName.Equals(databaseName));
            return fullSyncDatabase;
        }

        //*** GetDocument ***//

        /// <summary>
        /// Returns the requested document.
        /// </summary>
        /// <param name="fullSyncDatatbase"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override Object HandleGetDocumentRequest(String fullSyncDatatbaseName, String docidParameterValue, Dictionary<String, Object> parameters)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullSyncDatatbaseName);
            Database database = fullSyncDatabase.GetDatabase();

            // Gets the document form the local database
            Document document = database.GetExistingDocument(docidParameterValue);            

            if (document != null)
            {
                // If there are attachments, compute for each one the url to local storage and add it to the attachment descriptor
                JObject attachmentsProperty = (JObject)document.GetProperty(FullSyncInterface.FULL_SYNC__ATTACHMENTS);
                if (attachmentsProperty != null)
                {
                    SavedRevision rev = document.CurrentRevision;
                    Assembly couchbaseLiteAssembly = Assembly.GetAssembly(typeof(Attachment));
                    Type attachmentInternalType = couchbaseLiteAssembly.GetType(FullSyncMobile.ATTACHMENT_INTERNAL_TYPE);
                    ConstructorInfo attachmentInternalConstructor = attachmentInternalType.GetConstructor(new Type[] { typeof(String), typeof(IDictionary<String, Object>) });
                    foreach (KeyValuePair<String, JToken> attachmentProperty in attachmentsProperty)
                    {
                        String attachmentName = attachmentProperty.Key;
                        Attachment attachment = rev.GetAttachment(attachmentName);
                        if (!attachment.Metadata.Keys.Contains(ATTACHMENT_PROPERTY_KEY_CONTENT_URL))
                        {
                            Object[] attachmentInternalConstructorParams = new Object[] { attachment.Name, attachment.Metadata };
                            Object attachmentInternal = attachmentInternalConstructor.Invoke(attachmentInternalConstructorParams);

                            PropertyInfo databaseProp = attachmentInternalType.GetProperty(FullSyncMobile.ATTACHMENT_INTERNAL_PROPERTY_DATABASE);
                            databaseProp.SetValue(attachmentInternal, database);

                            PropertyInfo urlProp = attachmentInternalType.GetProperty(FullSyncMobile.ATTACHMENT_INTERNAL_PROPERTY_CONTENT_URL);
                            Object contentUrl = urlProp.GetValue(attachmentInternal, null);
                            if (contentUrl != null && contentUrl is Uri)
                            {
                                Uri uri = (Uri)contentUrl;
                                String absoluteUri = C8oUtils.UrlDecode(uri.AbsoluteUri);
                                String absolutePath = C8oUtils.UrlDecode(uri.AbsolutePath);
                                attachment.Metadata.Add(ATTACHMENT_PROPERTY_KEY_CONTENT_URL, absoluteUri);
                                if (attachmentProperty.Value is JObject)
                                {
                                    (attachmentProperty.Value as JObject).Add(ATTACHMENT_PROPERTY_KEY_CONTENT_URL, absoluteUri);
                                }
                                attachment.Metadata.Add("content_path", absolutePath);
                                if (attachmentProperty.Value is JObject)
                                {
                                    (attachmentProperty.Value as JObject).Add("content_path", absolutePath);
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
        public override Object HandleDeleteDocumentRequest(String fullSyncDatatbaseName, String docidParameterValue, Dictionary<String, Object> parameters)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullSyncDatatbaseName);

            String revParameterValue = C8oUtils.GetParameterStringValue(parameters, FullSyncDeleteDocumentParameter.REV.name, false);

            Document document = fullSyncDatabase.GetDatabase().GetExistingDocument(docidParameterValue);
            if (document == null)
            {
                throw new C8oRessourceNotFoundException(C8oExceptionMessage.RessourceNotFound("requested document"));
            }

            String documentRevision = document.CurrentRevisionId;

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
            Boolean deleted = document.Deleted;

            return new FullSyncDocumentOperationResponse(docidParameterValue, revParameterValue, deleted);
        }

        //*** PostDocument ***//

        public override Object HandlePostDocumentRequest(String fullSyncDatabaseName, FullSyncPolicy fullSyncPolicy, Dictionary<String, Object> parameters)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullSyncDatabaseName);
            
            // Gets the subkey separator parameter
            String subkeySeparatorParameterValue = C8oUtils.GetParameterStringValue(parameters, FullSyncPostDocumentParameter.SUBKEY_SEPARATOR.name, false);
            if (subkeySeparatorParameterValue == null)
            {
                subkeySeparatorParameterValue = ".";
            }

            // Filters and modifies wrong properties
		    Dictionary<String, Object> newProperties = new Dictionary<String, Object>();
		    foreach (KeyValuePair<String, Object> parameter in parameters) {
			    String parameterKey = parameter.Key;

                // Ignores parameters beginning with "__" or "_use_" 
                if (parameterKey.StartsWith("__"))
                {
				    continue;
			    }
                if (parameterKey.StartsWith("_use_"))
                {
				    continue;
			    }
			
                // Retrieves ???
			    Object objectParameterValue = C8oUtils.GetParameterJsonValue(parameter);
			    // Checks if the parameter name is splittable
                String[] paths = parameterKey.Split(new String[] { subkeySeparatorParameterValue }, StringSplitOptions.None); // Regex.Split(parameterKey, subkeySeparatorParameterValue);
			    if (paths.Length > 1) {
                    // The first substring becomes the key
                    parameterKey = paths[0];
                    // Next substrings create a hierarchy which will becomes json subkeys  
				    int count = paths.Length - 1;
				    while (count > 0) {
                        Dictionary<String, Object> tmpObject = new Dictionary<String, Object>();
					    tmpObject.Add(paths[count], objectParameterValue);
					    objectParameterValue = tmpObject;
					    count--;
				    }
			    }
			    newProperties.Add(parameterKey, objectParameterValue);
		    }
            Document createdDocument = CblEnum.PostDocument(fullSyncPolicy, fullSyncDatabase, newProperties);
            // Document createdDocument = fullSyncPolicy.PostDocument(fullSyncDatabase, newProperties);
            String documentId = createdDocument.Id;
            String currentRevision = createdDocument.CurrentRevisionId;
            return new FullSyncDocumentOperationResponse(documentId, currentRevision, true);
        }

        //*** GetAllDocuments ***//

        public override Object HandleAllDocumentsRequest(String fullSyncDatabaseName, Dictionary<String, Object> parameters)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullSyncDatabaseName);
            Database database = fullSyncDatabase.GetDatabase();

            
            // !!! TMP !!!
            if (parameters.ContainsKey("count"))
            {
                JObject json = new JObject();
                json.Add("count", database.DocumentCount);
                return json;
            }
            // !!! TMP !!!


            // Creates the fullSync query and add parameters to it
            Query query = database.CreateAllDocumentsQuery();
            
            FullSyncMobile.AddParametersToQuery(query, parameters);

            QueryEnumerator result = query.Run();
            /*
            if (parameters["include_docs"] == (true as Object))
            {
                IEnumerator<QueryRow> queryRows = queryEnumerator.GetEnumerator();
                while (queryRows.MoveNext())
                {
                    QueryRow queryRow = queryRows.Current;
                    JObject queryRowJson = FullSyncTranslator.DictionaryToJson(queryRow.AsJSONDictionary());
                    rowsArray.Add(queryRowJson);
                }
            }
            */
            return result;
        }

        //*** GetView ***//

        public override Object HandleGetViewRequest(String fullSyncDatabaseName, String ddocParameterValue, String viewParameterValue, Dictionary<String, Object> parameters)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullSyncDatabaseName);
            Database database = fullSyncDatabase.GetDatabase();

            // Gets the view depending to its programming language (Javascript / C#)
            Couchbase.Lite.View view;
            if (ddocParameterValue != null)
            {
                // Javascript view
                view = this.CheckAndCreateJavaScriptView(viewParameterValue, ddocParameterValue, database);
            }
            else
            {
                // C# view
                view = database.GetView(viewParameterValue);
            }
            if (view == null)
            {
                // Error
            }

            // Creates the fullSync query and add parameters to it
            Query query = view.CreateQuery();
            FullSyncMobile.AddParametersToQuery(query, parameters);

            QueryEnumerator result = query.Run();
            return result;
        }

        //*** Sync, ReplicatePull, ReplicatePush ***//

        public override Object HandleSyncRequest(String fullSyncDatabaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullSyncDatabaseName);
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

        public override Object HandleReplicatePullRequest(String fullSyncDatabaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullSyncDatabaseName);
            try
            {
                fullSyncDatabase.StartPullReplication(parameters, c8oResponseListener);
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }
            return VoidResponse.GetInstance();
        }

        public override Object HandleReplicatePushRequest(String fullSyncDatabaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullSyncDatabaseName);
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

        public override Object HandleResetDatabaseRequest(String fullsyncDatabaseName)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullsyncDatabaseName); // this.FindDatabase(fullsyncDatabaseName);
            if (fullSyncDatabase != null)
            {
                fullSyncDatabase.GetDatabase().Delete();
                fullSyncDatabases.Remove(fullSyncDatabase);
            }
		    return new FullSyncDefaultResponse(true);
	    }

        public override Object HandleCreateDatabaseRequest(String fullsyncDatabaseName)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(fullsyncDatabaseName);
            return new FullSyncDefaultResponse(true);
        }

        public override Object HandleDestroyDatabaseRequest(String fullsyncDatabaseName)
        {
            Database db = manager.GetDatabase(fullsyncDatabaseName);
            if (db != null)
            {
                manager.ForgetDatabase(db);
            }
            return new FullSyncDefaultResponse(true);
        }

        public CblDatabase GetOrCreateFullSyncDatabase(String databaseName)
        {
            // Searches if the database already exists
            /*foreach (FullSyncDatabase existingFullSyncDatabase in this.fullSyncDatabases)
            {
                if (existingFullSyncDatabase.GetDatabaseName().Equals(databaseName))
                {
                    return existingFullSyncDatabase;
                }
            }*/
            CblDatabase existingFullSyncDatabase = this.FindDatabase(databaseName);
            if (existingFullSyncDatabase != null)
            {
                return existingFullSyncDatabase;
            }

            // Creates a new database
            CblDatabase fullSyncDatabase = new CblDatabase(this.manager, databaseName, this.fullSyncDatabaseUrlBase, this.c8o);
            // this.fullSyncDatabases.Add(fullSyncDatabase);
            return fullSyncDatabase;
        }

        //*** JavaScript View ***//

        private Couchbase.Lite.View CompileView(String viewName, JObject viewProps, Database database)
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
            //String tmp = "";
            //foreach (Type type in couchbaseLiteAssembly.ExportedTypes)
            //{
            //    tmp = tmp + type.Namespace + " // " + type.Name + " \n";
            //}
            //Type jsViewCompilerType = couchbaseLiteAssembly.GetType(FullSyncMobile.JS_VIEW_COMPILER_TYPE);
            //ConstructorInfo[] constructors = jsViewCompilerType.GetConstructors();
            // ConstructorInfo attachmentInternalConstructor = attachmentInternalType.GetConstructor(new Type[] { typeof(String), typeof(IDictionary<String, Object>) });
            // Object attachmentInternal = attachmentInternalConstructor.Invoke(attachmentInternalConstructorParams);

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

        private Couchbase.Lite.View CheckAndCreateJavaScriptView(String viewName, String designDocumentName, Database database)
        {
            String tdViewName = designDocumentName + "/" + viewName;
            Couchbase.Lite.View view = database.GetExistingView(tdViewName);
            if (view == null || view.Map == null)
            {
                // No TouchDB view is defined, or it hasn't had a map block assigned
                // Searches in the design document if there is a CouchDB view definition we can compile

                Document designDocument = database.GetExistingDocument(FullSyncInterface.FULL_SYNC_DDOC_PREFIX + "/" + designDocumentName);
                JObject views = (JObject) designDocument.GetProperty(FULL_SYNC_VIEWS);
                JToken viewProps;
                if (!views.TryGetValue(viewName, out viewProps))
                {
                    return null;
                }
                view = this.CompileView(viewName, (JObject) viewProps, database);

                // ???
            }

            return view;
        }

        //*** Local cache ***//

        public override LocalCacheResponse GetResponseFromLocalCache(String c8oCallRequestIdentifier)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(C8o.LOCAL_CACHE_DATABASE_NAME);
            Document localCacheDocument = fullSyncDatabase.GetDatabase().GetExistingDocument(c8oCallRequestIdentifier);

            if (localCacheDocument == null)
            {
                throw new C8oUnavailableLocalCacheException(C8oExceptionMessage.ToDo());
            }

            IDictionary<String, Object> properties = localCacheDocument.Properties;

            String response;
            String responseTypeStr;
            ResponseType responseType;
            long expirationDate;
            if (!C8oUtils.TryGetParameterObjectValue<String>(properties, C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE, out response) || 
                !C8oUtils.TryGetParameterObjectValue<String>(properties, C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE, out responseTypeStr) ||
                !ResponseType.TryGetResponseType(responseTypeStr, out responseType))
            {
                throw new C8oUnavailableLocalCacheException(C8oExceptionMessage.invalidLocalCacheResponseInformation());
            }
            if (!C8oUtils.TryGetParameterObjectValue<long>(properties, C8o.LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE, out expirationDate))
            {
                expirationDate = -1;
            }

            return new LocalCacheResponse(response, responseType, expirationDate);
        }


        public override void SaveResponseToLocalCache(String c8oCallRequestIdentifier, LocalCacheResponse localCacheResponse)
        {
            CblDatabase fullSyncDatabase = this.GetOrCreateFullSyncDatabase(C8o.LOCAL_CACHE_DATABASE_NAME);
            Document localCacheDocument = fullSyncDatabase.GetDatabase().GetDocument(c8oCallRequestIdentifier);

            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE, localCacheResponse.Response);
            properties.Add(C8o.LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE, localCacheResponse.ResponseType.Value);
            if (localCacheResponse.ExpirationDate > 0)
            {
                // long expirationDate = C8oUtils.GetUnixEpochTime(DateTime.Now) + timeToLive;
                properties.Add(C8o.LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE, localCacheResponse.ExpirationDate);
            }

            SavedRevision currentRevision = localCacheDocument.CurrentRevision;
            if (currentRevision != null)
            {
                properties.Add(FullSyncInterface.FULL_SYNC__REV, currentRevision.Id);
            }

            localCacheDocument.PutProperties(properties);
        }

        //*** Other ***//
        
        /// <summary>
        /// Adds known parameters to the fullSync query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        private static void AddParametersToQuery(Query query, Dictionary<String, Object> parameters)
        {
            foreach (KeyValuePair<String, Object> parameter in parameters)
            {
                FullSyncRequestParameter fullSyncRequestParameter = FullSyncRequestParameter.GetFullSyncRequestParameter(parameter.Key);
                // If the parameter corresponds to a FullSyncRequestParameter
                if (fullSyncRequestParameter != null)
                {
                    Object parameterValue = parameter.Value;
                    //Object objectParameterValue = parameterValue;
                    //if (parameterValue is String)
                    //{
                    //    // Passer par une fonction ??,,
                    //    objectParameterValue = JsonConvert.DeserializeObject(parameterValue as String, fullSyncRequestParameter.type);
                    //}
                    // fullSyncRequestParameter.AddToQuery(query, objectParameterValue);
                    CblEnum.AddToQuery(query, fullSyncRequestParameter, parameterValue);
                }
            }
        }

        //protected override FullSyncDatabase2 CreateFullSyncDatabase(string databaseName)
        //{
        //    return new FullSyncDatabase(this.manager, databaseName, this.fullSyncDatabaseUrlBase, this.c8o);
        //}

        public static void Init()
        {
            //C8o.FullSyncInterfaceUsed = new FullSyncMobile().GetType();
            C8o.FullSyncInterfaceUsed = Type.GetType("Convertigo.SDK.FullSync.FullSyncMobile", true);
        }

    }

}