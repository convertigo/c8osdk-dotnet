using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Convertigo.SDK.Internal
{
    internal abstract class C8oFullSync
    {
        public readonly static string FULL_SYNC_URL_PATH = "/fullsync/";
        /// <summary>
        /// The project requestable value to execute a fullSync request.
        /// </summary>
        public readonly static string FULL_SYNC_PROJECT = "fs://";
        public readonly static string FULL_SYNC__ID = "_id";
        public readonly static string FULL_SYNC__REV = "_rev";
        public readonly static string FULL_SYNC__ATTACHMENTS = "_attachments";

        public readonly static string FULL_SYNC_DDOC_PREFIX = "_design";
        public readonly static string FULL_SYNC_VIEWS = "views";

        protected C8o c8o;
        protected string fullSyncDatabaseUrlBase;
        protected string localSuffix;

        public virtual void Init(C8o c8o)
        {
            this.c8o = c8o;
            fullSyncDatabaseUrlBase = c8o.EndpointConvertigo + C8oFullSync.FULL_SYNC_URL_PATH;
            localSuffix = (c8o.FullSyncLocalSuffix != null) ? c8o.FullSyncLocalSuffix : "_device";
        }

        //*** Request handlers ***//

        public async Task<object> HandleFullSyncRequest(IDictionary<string, object> parameters, C8oResponseListener listener)
        {
            // Gets the project and the sequence parameter in order to know which database and which fullSyncrequestable to use
            string projectParameterValue = C8oUtils.PeekParameterStringValue(parameters, C8o.ENGINE_PARAMETER_PROJECT, true);

            if (!projectParameterValue.StartsWith(FULL_SYNC_PROJECT))
            {
                throw new ArgumentException(C8oExceptionMessage.InvalidParameterValue(projectParameterValue, "its don't start with " + FULL_SYNC_PROJECT));
            }

            String fullSyncRequestableValue = C8oUtils.PeekParameterStringValue(parameters, C8o.ENGINE_PARAMETER_SEQUENCE, true);
            // Gets the fullSync requestable and gets the response from this requestable
            FullSyncRequestable fullSyncRequestable = FullSyncRequestable.GetFullSyncRequestable(fullSyncRequestableValue);
            if (fullSyncRequestable == null)
            {
                throw new ArgumentException(C8oExceptionMessage.InvalidParameterValue(C8o.ENGINE_PARAMETER_PROJECT, C8oExceptionMessage.UnknownValue("fullSync requestable", fullSyncRequestableValue)));
            }

            // Gets the database name if this is not specified then if takes the default database name
            String databaseName = projectParameterValue.Substring(C8oFullSync.FULL_SYNC_PROJECT.Length);
            if (databaseName.Length < 1)
            {
                databaseName = c8o.DefaultDatabaseName;
                if (databaseName == null)
                {
                    throw new ArgumentException(C8oExceptionMessage.InvalidParameterValue(C8o.ENGINE_PARAMETER_PROJECT, C8oExceptionMessage.MissingValue("fullSync database name")));
                }
            }

            Object response;
            try
            {
                response = await fullSyncRequestable.HandleFullSyncRequest(this, databaseName, parameters, listener);
            }
            catch (C8oException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.FullSyncRequestFail(), e);
            }

            if (response == null)
            {
                throw new C8oException(C8oExceptionMessage.couchNullResult());
            }

            response = HandleFullSyncResponse(response, listener);
            return response;
        }

        /// <summary>
        /// Handles the fullSync response depending to the C8oResponseListener.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="parameters"></param>
        /// <param name="c8oResponseListener"></param>
        public virtual object HandleFullSyncResponse(object response, C8oResponseListener listener)
        {
            if (response is JObject)
            {
                if (listener is C8oResponseXmlListener)
                {
                    response = C8oFullSyncTranslator.FullSyncJsonToXml(response as JObject);
                }
            }

            return response;
        }

        /// <summary>
        /// Returns the requested document.
        /// </summary>
        /// <param name="fullSyncDatatbase"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public abstract Task<object> HandleGetDocumentRequest(string fullSyncDatatbaseName, string docid, IDictionary<string, object> parameters);

        //*** DeleteDocument ***//

        /// <summary>
        /// Deletes an existing document from the local database.
        /// </summary>
        /// <param name="fullSyncDatabase"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public abstract Task<object> HandleDeleteDocumentRequest(string fullSyncDatatbaseName, string docid, IDictionary<string, object> parameters);

        //*** PostDocument ***//

        public abstract Task<object> HandlePostDocumentRequest(string fullSyncDatatbaseName, FullSyncPolicy fullSyncPolicy, IDictionary<string, object> parameters);

        //*** GetAllDocuments ***//

        public abstract Task<object> HandleAllDocumentsRequest(string fullSyncDatatbaseName, IDictionary<string, object> parameters);

        //*** GetView ***//

        public abstract Task<object> HandleGetViewRequest(string fullSyncDatatbaseName, string ddoc, string view, IDictionary<string, object> parameters);

        //*** Sync, ReplicatePull, ReplicatePush ***//

        public abstract Task<object> HandleSyncRequest(string fullSyncDatatbaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener);

        public abstract Task<object> HandleReplicatePullRequest(string fullSyncDatatbaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener);

        public abstract Task<object> HandleReplicatePushRequest(string fullSyncDatatbaseName, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener);

        //*** Reset ***//

        public abstract Task<object> HandleResetDatabaseRequest(string fullSyncDatatbaseName);

        //*** Create ***//
        public abstract Task<object> HandleCreateDatabaseRequest(string fullSyncDatatbaseName);

        //*** Destroy ***//
        public abstract Task<object> HandleDestroyDatabaseRequest(string fullSyncDatatbaseName);

        //*** Local cache ***//

        /// <summary>
        /// Gets the c8o call response stored into the local cache thanks to the c8o call request identifier.
        /// </summary>
        /// <param name="c8oCallRequestIdentifier"></param>
        /// <returns></returns>
        public abstract Task<C8oLocalCacheResponse> GetResponseFromLocalCache(string c8oCallRequestIdentifier);

        /// <summary>
        /// Saves the c8o call response into the local cache.
        /// </summary>
        /// <param name="c8oCallRequestIdentifier"></param>
        /// <param name="responseString"></param>
        /// <param name="responseType"></param>
        /// <param name="localCacheTimeToLive"></param>
        public abstract Task SaveResponseToLocalCache(string c8oCallRequestIdentifier, C8oLocalCacheResponse localCacheResponse);

        /// <summary>
        /// Checks if request parameters correspond to a fullSync request.
        /// </summary>
        public static bool IsFullSyncRequest(IDictionary<string, object> requestParameters)
        {
            // Check if there is one parameter named "__project" and if its value starts with "fs://"
            string parameterValue = C8oUtils.GetParameterStringValue(requestParameters, C8o.ENGINE_PARAMETER_PROJECT, false);
            if (parameterValue != null)
            {
                return parameterValue.StartsWith(FULL_SYNC_PROJECT);
            }
            return false;
        }
    }
}
