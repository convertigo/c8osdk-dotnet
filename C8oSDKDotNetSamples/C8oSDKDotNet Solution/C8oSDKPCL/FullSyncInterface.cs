using Convertigo.SDK.Exceptions;
using Convertigo.SDK.FullSync.Enums;
using Convertigo.SDK.Listeners;
using Convertigo.SDK.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.FullSync
{

    public abstract class FullSyncInterface
    {
        //*** Constants ***//

        public static String FULL_SYNC_URL_PATH = "/fullsync/";
        /// <summary>
        /// The project requestable value to execute a fullSync request.
        /// </summary>
        public static String FULL_SYNC_PROJECT = "fs://";
        public static String FULL_SYNC__ID = "_id";
        public static String FULL_SYNC__REV = "_rev";

        public static readonly String FULL_SYNC_DDOC_PREFIX = "_design";
        public static readonly String FULL_SYNC_VIEWS = "views";
        public static readonly String FULL_SYNC__ATTACHMENTS = "_attachments";

        public String defaultFullSyncDatabaseName;

        protected String localSuffix;

        public virtual void Init(C8o c8o, C8oSettings c8oSettings, String endpointFirstPart)
        {
            this.defaultFullSyncDatabaseName = c8oSettings.defaultFullSyncDatabaseName;
            localSuffix = (c8oSettings.fullSyncLocalSuffix != null) ? c8oSettings.fullSyncLocalSuffix : "_device";
        }

        //*** Request handlers ***//

        public Object HandleFullSyncRequest(Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            //*** Checks parameters validity ***//
            Dictionary<String, Object> data = new Dictionary<String, Object>(parameters);

            // Gets the project and the sequence parameter in order to know which database and which fullSyncrequestable to use
            String projectParameterValue = C8oUtils.PeekParameterStringValue(data, C8o.ENGINE_PARAMETER_PROJECT, true);
            String fullSyncRequestableValue = C8oUtils.PeekParameterStringValue(data, C8o.ENGINE_PARAMETER_SEQUENCE, true);

            // Gets the database name if this is not specified then if takes the default database name
            String databaseName = projectParameterValue.Substring(FullSyncInterface.FULL_SYNC_PROJECT.Length);
            if (databaseName.Length < 1)
            {
                if (this.defaultFullSyncDatabaseName == null)
                {
                    throw new ArgumentException(C8oExceptionMessage.InvalidParameterValue(C8o.ENGINE_PARAMETER_PROJECT, C8oExceptionMessage.MissingValue("fullSync database name")));
                }
                databaseName = this.defaultFullSyncDatabaseName;
            }

            // Gets the fullSync requestable and gets the response from this requestable
            FullSyncRequestable fullSyncRequestable = FullSyncRequestable.GetFullSyncRequestable(fullSyncRequestableValue);
            if (fullSyncRequestable == null)
            {
                throw new ArgumentException(C8oExceptionMessage.InvalidParameterValue(C8o.ENGINE_PARAMETER_PROJECT, C8oExceptionMessage.UnknownValue("fullSync requestable", fullSyncRequestableValue)));
            }

            Object response;
            try
            {
                response = fullSyncRequestable.HandleFullSyncRequest(this, databaseName, data, c8oResponseListener);
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }
            if (response == null)
            {
                throw new C8oException(C8oExceptionMessage.couchNullResult());
            }

            // Handles the response
            this.HandleFullSyncResponse(response, parameters, c8oResponseListener);
            return response;
        }

        /// <summary>
        /// Handles the fullSync response depending to the C8oResponseListener.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="parameters"></param>
        /// <param name="c8oResponseListener"></param>
        public abstract void HandleFullSyncResponse(Object response, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener);

        //*** GetDocument ***//

        /// <summary>
        /// Returns the requested document.
        /// </summary>
        /// <param name="fullSyncDatatbase"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public abstract Object HandleGetDocumentRequest(String fullSyncDatatbaseName, String docidParameterValue, Dictionary<String, Object> parameters);

        //*** DeleteDocument ***//

        /// <summary>
        /// Deletes an existing document from the local database.
        /// </summary>
        /// <param name="fullSyncDatabase"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public abstract Object HandleDeleteDocumentRequest(String fullSyncDatatbaseName, String docidParameterValue, Dictionary<String, Object> parameters);

        //*** PostDocument ***//

        public abstract Object HandlePostDocumentRequest(String fullSyncDatatbaseName, FullSyncPolicy fullSyncPolicy, Dictionary<String, Object> parameters);

        //*** GetAllDocuments ***//

        public abstract Object HandleAllDocumentsRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters);

        //*** GetView ***//

        public abstract Object HandleGetViewRequest(String fullSyncDatatbaseName, String ddocParameterValue, String viewParameterValue, Dictionary<String, Object> parameters);

        //*** Sync, ReplicatePull, ReplicatePush ***//

        public abstract Object HandleSyncRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener);

        public abstract Object HandleReplicatePullRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener);

        public abstract Object HandleReplicatePushRequest(String fullSyncDatatbaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener);

        //*** Reset ***//

        public abstract Object HandleResetDatabaseRequest(String fullSyncDatatbaseName);

        //*** Create ***//
        public abstract Object HandleCreateDatabaseRequest(String fullSyncDatatbaseName);

        //*** Destroy ***//
        public abstract Object HandleDestroyDatabaseRequest(String fullSyncDatatbaseName);

        //*** Local cache ***//

        /// <summary>
        /// Gets the c8o call response stored into the local cache thanks to the c8o call request identifier.
        /// </summary>
        /// <param name="c8oCallRequestIdentifier"></param>
        /// <returns></returns>
        public abstract LocalCacheResponse GetResponseFromLocalCache(String c8oCallRequestIdentifier);

        /// <summary>
        /// Saves the c8o call response into the local cache.
        /// </summary>
        /// <param name="c8oCallRequestIdentifier"></param>
        /// <param name="responseString"></param>
        /// <param name="responseType"></param>
        /// <param name="timeToLive"></param>
        public abstract void SaveResponseToLocalCache(String c8oCallRequestIdentifier, LocalCacheResponse localCacheResponse);
    }

    //internal class DefaultFullSyncInterface2 : FullSyncInterface
    //{
    //    public DefaultFullSyncInterface2() : base() { }

    //    public override void Init(C8o c8o, C8oSettings c8oSettings, String endpointFirstPart) { }

    //    public override object HandleFullSyncRequest(Dictionary<String, Object> parameters, C8oResponseListener responseListener)
    //    {
    //        throw new NotImplementedException(C8oExceptionMessage.NotImplementedFullSyncInterface());
    //    }

    //    public override Object GetResponseFromLocalCache(String c8oCallRequestIdentifier)
    //    {
    //        throw new NotImplementedException(C8oExceptionMessage.NotImplementedFullSyncInterface());
    //    }

    //    public override void SaveResponseToLocalCache(String c8oCallRequestIdentifier, String responseString, String responseType, int timeToLive)
    //    {
    //        throw new NotImplementedException(C8oExceptionMessage.NotImplementedFullSyncInterface());
    //    }
    //}
}
