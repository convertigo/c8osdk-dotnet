using System;
using System.Collections.Generic;
using System.Text;

namespace Convertigo.SDK.FullSync.Responses
{
    class FullSyncResponse
    {
        /// <summary>
        /// The response key indicating the operation status.
        /// </summary>
        public static readonly String RESPONSE_KEY_OK = "ok";
        /// <summary>
        /// The response key indicating the document ID.
        /// </summary>
        public static readonly String RESPONSE_KEY_DOCUMENT_ID = "id";
        /// <summary>
        /// The response key indicating the document revision.
        /// </summary>
        public static readonly String RESPONSE_KEY_DOCUMENT_REVISION = "rev";
    }

    //*** Response classes ***//

    /**
    * Returned by a fullSync operation without return data.
    */
    public class FullSyncAbstractResponse
    {
        public Boolean operationStatus;

        public FullSyncAbstractResponse(Boolean operationStatus)
        {
            this.operationStatus = operationStatus;
        }

        public virtual Dictionary<String, Object> GetProperties()
        {
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(FullSyncResponse.RESPONSE_KEY_OK, this.operationStatus);
            return properties;
        }
    }

    public class FullSyncDefaultResponse : FullSyncAbstractResponse
    {
        public FullSyncDefaultResponse(Boolean operationStatus)
            : base(operationStatus)
        {

        }
    }

    /// <summary>
    /// Returned by a fullSync document operation without return data.
    /// </summary>
    public class FullSyncDocumentOperationResponse : FullSyncAbstractResponse
    {
        public String documentId;
        public String documentRevision;

        //internal Dictionary<String, Object> Properties
        //{
        //    get
        //    {
        //        Dictionary<String, Object> properties = base.GetProperties();
        //        properties.Add(FullSyncResponse.RESPONSE_KEY_DOCUMENT_ID, this.documentId);
        //        properties.Add(FullSyncResponse.RESPONSE_KEY_DOCUMENT_REVISION, this.documentRevision);
        //        return properties;
        //    }
        //}

        public FullSyncDocumentOperationResponse(String documentId, String documentRevision, Boolean operationStatus)
            : base(operationStatus)
        {
            this.documentId = documentId;
            this.documentRevision = documentRevision;
        }

        public override Dictionary<String, Object> GetProperties()
        {
            Dictionary<String, Object> properties = base.GetProperties();
            properties.Add(FullSyncResponse.RESPONSE_KEY_DOCUMENT_ID, this.documentId);
            properties.Add(FullSyncResponse.RESPONSE_KEY_DOCUMENT_REVISION, this.documentRevision);
            return properties;
        }
    }
}
