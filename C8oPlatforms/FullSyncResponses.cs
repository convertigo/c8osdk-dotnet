using System.Collections.Generic;

namespace Convertigo.SDK.Internal
{
    internal class FullSyncResponse
    {
        /// <summary>
        /// The response key indicating the operation status.
        /// </summary>
        public static readonly string RESPONSE_KEY_OK = "ok";
        /// <summary>
        /// The response key indicating the document ID.
        /// </summary>
        public static readonly string RESPONSE_KEY_DOCUMENT_ID = "id";
        /// <summary>
        /// The response key indicating the document revision.
        /// </summary>
        public static readonly string RESPONSE_KEY_DOCUMENT_REVISION = "rev";
    }

    //*** Response classes ***//

    /**
    * Returned by a fullSync operation without return data.
    */
    public class FullSyncAbstractResponse
    {
        public bool operationStatus;

        public FullSyncAbstractResponse(bool operationStatus)
        {
            this.operationStatus = operationStatus;
        }

        public virtual IDictionary<string, object> GetProperties()
        {
            var properties = new Dictionary<string, object>();
            properties[FullSyncResponse.RESPONSE_KEY_OK] = operationStatus;
            return properties;
        }
    }

    public class FullSyncDefaultResponse : FullSyncAbstractResponse
    {
        public FullSyncDefaultResponse(bool operationStatus)
            : base(operationStatus)
        {

        }
    }

    /// <summary>
    /// Returned by a fullSync document operation without return data.
    /// </summary>
    public class FullSyncDocumentOperationResponse : FullSyncAbstractResponse
    {
        public string documentId;
        public string documentRevision;

        //internal Dictionary<string, object> Properties
        //{
        //    get
        //    {
        //        Dictionary<string, object> properties = base.GetProperties();
        //        properties.Add(FullSyncResponse.RESPONSE_KEY_DOCUMENT_ID, this.documentId);
        //        properties.Add(FullSyncResponse.RESPONSE_KEY_DOCUMENT_REVISION, this.documentRevision);
        //        return properties;
        //    }
        //}

        public FullSyncDocumentOperationResponse(string documentId, string documentRevision, bool operationStatus)
            : base(operationStatus)
        {
            this.documentId = documentId;
            this.documentRevision = documentRevision;
        }

        public override IDictionary<string, object> GetProperties()
        {
            var properties = base.GetProperties();
            properties.Add(FullSyncResponse.RESPONSE_KEY_DOCUMENT_ID, this.documentId);
            properties.Add(FullSyncResponse.RESPONSE_KEY_DOCUMENT_REVISION, this.documentRevision);
            return properties;
        }
    }
}
