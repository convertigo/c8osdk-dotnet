using Convertigo.SDK.Exceptions;
using Convertigo.SDK.Utils;
using Couchbase.Lite;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Convertigo.SDK.FullSync.Enums
{

    internal class C8oFullSyncCblEnum
    {
        private static Boolean initialized = false;
        private static IDictionary<FullSyncRequestParameter, Action<Query, Object>> fullSyncRequestParameters;
        private static IDictionary<FullSyncPolicy, Func<Database, IDictionary<string, object>, Document>> fullSyncPolicies;
        private static IDictionary<FullSyncReplicationParameter, Action<Replication, Object>> fullSyncReplicationParameters;

        private static void Init()
        {
            if (!initialized) 
            {
                initialized = true;
                InitFullSyncRequestParameters();
                InitFullSyncPolicies();
                InitFullSyncReplicationDatabaseParameters();
            }
        }

        internal static void AddToQuery(Query query, FullSyncRequestParameter requestParameter, Object value)
        {
            Init();
            // Checks if the value type is String and the request parameter type is not
            if (typeof(String).IsAssignableFrom(value.GetType()) && !typeof(String).IsAssignableFrom(requestParameter.type))
            {
                // Tries to convert the string to the request parameter type
                value = C8oTranslator.StringToObject((String)value, requestParameter.type);
            }
            // Checks if the type is valid
            if (requestParameter.type.IsAssignableFrom(value.GetType()))
            {
                // No reasons to fail
                Action<Query, Object> addToQueryOp = null;
                if (fullSyncRequestParameters.TryGetValue(requestParameter, out addToQueryOp))
                {
                    addToQueryOp(query, value);
                }
            }
            else
            {
                throw new ArgumentException(C8oExceptionMessage.ToDo());
            }
        }

        internal static Document PostDocument(FullSyncPolicy policy, Database database, IDictionary<string, object> newProperties)
        {
            Init();
            Func<Database, IDictionary<string, object>, Document> postDocumentOp = null;
            if (fullSyncPolicies.TryGetValue(policy, out postDocumentOp))
            {
                return postDocumentOp(database, newProperties);
            }
            else
            {
                throw new ArgumentException(C8oExceptionMessage.ToDo());
            }
        }

        internal static void SetReplication(FullSyncReplicationParameter replicationParameter, Replication replication, Object value)
        {
            Init();
            // Checks if the value type is String and the request parameter type is not
            if (typeof(String).IsAssignableFrom(value.GetType()) && !typeof(String).IsAssignableFrom(replicationParameter.type))
            {
                try
                {
                    // Tries to convert the string to the request parameter type
                    value = C8oTranslator.StringToObject((String)value, replicationParameter.type);
                }
                catch (Exception e)
                {
                    throw new C8oException(C8oExceptionMessage.ToDo(), e);
                }
            }
            // Checks if the type is valid
            if (replicationParameter.type.IsAssignableFrom(value.GetType()))
            {
                // No reasons to fail
                Action<Replication, Object> setReplicationOp = null;
                if (fullSyncReplicationParameters.TryGetValue(replicationParameter, out setReplicationOp))
                {
                    setReplicationOp(replication, value);
                }
            }
            else
            {
                throw new ArgumentException(C8oExceptionMessage.ToDo());
            }           
        }

        //*** FullSyncRequestParameter ***//

        private static void InitFullSyncRequestParameters()
        {
            fullSyncRequestParameters = new Dictionary<FullSyncRequestParameter, Action<Query, Object>>();
            FullSyncRequestParameter requestParameter;
            Action<Query, Object> action;
            // DESCENDING
            requestParameter = FullSyncRequestParameter.DESCENDING;
            action = (query, value) => 
            {
                query.Descending = (Boolean)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // ENDKEY
            requestParameter = FullSyncRequestParameter.ENDKEY;
            action = (query, value) =>
            {
                query.EndKey = value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // ENDKEY_DOCID
            requestParameter = FullSyncRequestParameter.ENDKEY_DOCID;
            action = (query, value) =>
            {
                query.EndKeyDocId = (String)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // GROUP_LEVEL
            requestParameter = FullSyncRequestParameter.GROUP_LEVEL;
            action = (query, value) =>
            {
                query.GroupLevel = (int)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // INCLUDE_DELETED
            requestParameter = FullSyncRequestParameter.INCLUDE_DELETED;
            action = (query, value) =>
            {
                query.IncludeDeleted = (Boolean)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // INDEX_UPDATE_MODE
            requestParameter = FullSyncRequestParameter.INDEX_UPDATE_MODE;
            action = (query, value) =>
            {
                String valueStr = (String)value;
                Array indexUpdateModeValues = Enum.GetValues(typeof(IndexUpdateMode));
                IEnumerator indexUpdateModeEnumerator = indexUpdateModeValues.GetEnumerator();
                while (indexUpdateModeEnumerator.MoveNext())
                {
                    IndexUpdateMode indexUpdateMode = (IndexUpdateMode)indexUpdateModeEnumerator.Current;
                    if (valueStr.Equals(indexUpdateMode.ToString()))
                    {
                        query.IndexUpdateMode = indexUpdateMode;
                        return;
                    }
                }
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // KEYS
            requestParameter = FullSyncRequestParameter.KEYS;
            action = (query, value) =>
            {
                query.Keys = (IEnumerable<Object>)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // LIMIT
            requestParameter = FullSyncRequestParameter.LIMIT;
            action = (query, value) =>
            {
                query.Limit = (int)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // INCLUDE_DOCS
            requestParameter = FullSyncRequestParameter.INCLUDE_DOCS;
            action = (query, value) =>
            {
                // missing include_docs !
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // MAP_ONLY
            requestParameter = FullSyncRequestParameter.MAP_ONLY;
            action = (query, value) =>
            {
                query.MapOnly = (Boolean)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // PREFETCH
            requestParameter = FullSyncRequestParameter.PREFETCH;
            action = (query, value) =>
            {
                query.Prefetch = (Boolean)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // SKIP
            requestParameter = FullSyncRequestParameter.SKIP;
            action = (query, value) =>
            {
                query.Skip = (int)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // STARTKEY
            requestParameter = FullSyncRequestParameter.STARTKEY;
            action = (query, value) =>
            {
                query.StartKey = value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
            // STARTKEY_DOCID
            requestParameter = FullSyncRequestParameter.STARTKEY_DOCID;
            action = (query, value) =>
            {
                query.StartKeyDocId = (String)value;
            };
            fullSyncRequestParameters.Add(requestParameter, action);
        }

        //*** FullSyncPolicies ***//

        private static void InitFullSyncPolicies()
        {
            fullSyncPolicies = new Dictionary<FullSyncPolicy, Func<Database, IDictionary<string, object>, Document>>();
            FullSyncPolicy policy;
            Func<Database, IDictionary<string, object>, Document> func;
            // CREATE
            policy = FullSyncPolicy.CREATE;
            func = (database, newProperties) =>
            {
                // Removes specials properties in order to create a new document
                newProperties.Remove(C8oFullSync.FULL_SYNC__ID);
                newProperties.Remove(C8oFullSync.FULL_SYNC__REV);

                Document createdDocument = database.CreateDocument();
                createdDocument.PutProperties(newProperties);
                return createdDocument;
            };
            fullSyncPolicies.Add(policy, func);
            // MERGE
            policy = FullSyncPolicy.MERGE;
            func = (database, newProperties) =>
            {
                // Gets the document ID
                String documentId = C8oUtils.GetParameterStringValue(newProperties, C8oFullSync.FULL_SYNC__ID, false);

                // Removes special properties in order to create a new document
                newProperties.Remove(C8oFullSync.FULL_SYNC__ID);
                newProperties.Remove(C8oFullSync.FULL_SYNC__REV);

                // Creates a new document or get an existing one (if the ID is specified)
                Document createdDocument;
                if (documentId == null)
                {
                    createdDocument = database.CreateDocument();
                }
                else
                {
                    createdDocument = database.GetDocument(documentId);
                }

                // Merges old properties with the new ones
                IDictionary<string, object> oldProperties = createdDocument.Properties;
                if (oldProperties != null)
                {
                    FullSyncUtils.MergeProperties(newProperties, oldProperties);
                }

                createdDocument.PutProperties(newProperties);

                return createdDocument;
            };
            fullSyncPolicies.Add(policy, func);
            // NONE
            policy = FullSyncPolicy.NONE;
            func = (database, newProperties) =>
            {
                Document createdDocument = (newProperties.ContainsKey(C8oFullSync.FULL_SYNC__ID)) ?
                    database.GetDocument(newProperties[C8oFullSync.FULL_SYNC__ID].ToString()) :
                    database.CreateDocument();
                createdDocument.PutProperties(newProperties);
                return createdDocument;
            };
            fullSyncPolicies.Add(policy, func);
            // OVERRIDE
            policy = FullSyncPolicy.OVERRIDE;
            func = (database, newProperties) =>
            {
                // Gets the document ID
                String documentId = C8oUtils.GetParameterStringValue(newProperties, C8oFullSync.FULL_SYNC__ID, false);

                // Removes special properties in order to create a new document
                newProperties.Remove(C8oFullSync.FULL_SYNC__ID);
                newProperties.Remove(C8oFullSync.FULL_SYNC__REV);

                // Creates a new document or get an existing one (if the ID is specified)
                Document createdDocument;
                if (documentId == null)
                {
                    createdDocument = database.CreateDocument();
                }
                else
                {
                    createdDocument = database.GetDocument(documentId);
                    // Must add the current revision to the properties
                    SavedRevision currentRevision = createdDocument.CurrentRevision;
                    if (currentRevision != null)
                    {
                        newProperties.Add(C8oFullSync.FULL_SYNC__REV, currentRevision.Id);
                    }
                }
                createdDocument.PutProperties(newProperties);
                return createdDocument;
            };
            fullSyncPolicies.Add(policy, func);
        }

        //*** FullSyncReplicationDatabaseParameters ***//

        private static void InitFullSyncReplicationDatabaseParameters() 
        {
            fullSyncReplicationParameters = new Dictionary<FullSyncReplicationParameter, Action<Replication, Object>>();
            FullSyncReplicationParameter replicationParameter;
            Action<Replication, Object> action;
            // CANCEL
            replicationParameter = FullSyncReplicationParameter.CANCEL;
            action = (replication, value) =>
            {

            };
            fullSyncReplicationParameters.Add(replicationParameter, action);
            // DOCIDS
            replicationParameter = FullSyncReplicationParameter.DOCIDS;
            action = (replication, value) =>
            {
                replication.DocIds = (IEnumerable<String>)value;
            };
            fullSyncReplicationParameters.Add(replicationParameter, action);
            // LIVE
            replicationParameter = FullSyncReplicationParameter.LIVE;
            action = (replication, value) =>
            {
                replication.Continuous = (Boolean)value;
            };
            fullSyncReplicationParameters.Add(replicationParameter, action);
        }
    }
}
