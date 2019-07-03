using Couchbase.Lite;
using System;
using System.Collections.Generic;

namespace Convertigo.SDK.Internal
{
    internal class C8oResponseCblListener : C8oResponseListener
    {
        /// <summary>
        /// Called back when
        /// </summary>
        public Action<Document, IDictionary<string, object>> OnDocumentResponse;
        public Action<QueryEnumerator, IDictionary<string, object>> OnQueryEnumeratorResponse;
        public Action<ReplicationChangeEventArgs, IDictionary<string, object>> OnReplicationChangeEventResponse;

        public C8oResponseCblListener(Action<Document, IDictionary<string, object>> OnDocumentResponse,
            Action<QueryEnumerator, IDictionary<string, object>> OnQueryEnumeratorResponse,
            Action<ReplicationChangeEventArgs, IDictionary<string, object>> OnReplicationChangeEventResponse)
        {
            this.OnDocumentResponse = OnDocumentResponse;
            this.OnQueryEnumeratorResponse = OnQueryEnumeratorResponse;
            this.OnReplicationChangeEventResponse = OnReplicationChangeEventResponse;
        }
    }
}