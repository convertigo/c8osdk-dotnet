using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Convertigo.SDK.Listeners;
using Couchbase.Lite;

namespace Convertigo.SDK.FullSync
{
    public class C8oCblResponseListener : C8oResponseListener
    {
        /// <summary>
        /// Called back when
        /// </summary>
        public Action<Document, Dictionary<String, Object>> OnDocumentResponse;
        public Action<QueryEnumerator, Dictionary<String, Object>> OnQueryEnumeratorResponse;
        public Action<ReplicationChangeEventArgs, Dictionary<String, Object>> OnReplicationChangeEventResponse;

        public C8oCblResponseListener(Action<Document, Dictionary<String, Object>> OnDocumentResponse,
            Action<QueryEnumerator, Dictionary<String, Object>> OnQueryEnumeratorResponse,
            Action<ReplicationChangeEventArgs, Dictionary<String, Object>> OnReplicationChangeEventResponse)
        {
            this.OnDocumentResponse = OnDocumentResponse;
            this.OnQueryEnumeratorResponse = OnQueryEnumeratorResponse;
            this.OnReplicationChangeEventResponse = OnReplicationChangeEventResponse;
        }
    }
}