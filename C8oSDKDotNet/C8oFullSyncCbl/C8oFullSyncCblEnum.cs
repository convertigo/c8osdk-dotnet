using Couchbase.Lite;
using System;
using System.Collections.Generic;

namespace Convertigo.SDK.Internal
{

    internal class C8oFullSyncCblEnum
    {
        private static bool initialized = false;
        private static IDictionary<FullSyncRequestParameter, Action<Query, object>> fullSyncRequestParameters;
        private static IDictionary<FullSyncPolicy, Func<Database, IDictionary<string, object>, Document>> fullSyncPolicies;
        private static IDictionary<FullSyncReplicationParameter, Action<Replication, object>> fullSyncReplicationParameters;

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

        internal static void AddToQuery(Query query, FullSyncRequestParameter requestParameter, object value)
        {
            Init();
            // Checks if the type is valid
            if (requestParameter.type.IsAssignableFrom(value.GetType()))
            {
                fullSyncRequestParameters[requestParameter](query, value);
            }
            else
            {
                throw new ArgumentException(C8oExceptionMessage.InvalidArgumentInvalidParameterType(requestParameter.name, "" + requestParameter.type, "" + value.GetType()));
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
                throw new ArgumentException(C8oExceptionMessage.UnknownFullSyncPolicy(policy));
            }
        }

        internal static void SetReplication(FullSyncReplicationParameter replicationParameter, Replication replication, object value)
        {
            Init();
            // Checks if the value type is String and the request parameter type is not
            if (typeof(string).IsAssignableFrom(value.GetType()) && !typeof(string).IsAssignableFrom(replicationParameter.type))
            {
                try
                {
                    // Tries to convert the string to the request parameter type
                    value = C8oTranslator.StringToObject(value as string, replicationParameter.type);
                }
                catch (Exception e)
                {
                    throw new C8oException(C8oExceptionMessage.ParseStringToObject(replicationParameter.type), e);
                }
            }
            // Checks if the type is valid
            if (replicationParameter.type.IsAssignableFrom(value.GetType()))
            {
                // No reasons to fail
                Action<Replication, object> setReplicationOp = null;
                if (fullSyncReplicationParameters.TryGetValue(replicationParameter, out setReplicationOp))
                {
                    setReplicationOp(replication, value);
                }
            }
            else
            {
                throw new ArgumentException(C8oExceptionMessage.InvalidArgumentInvalidParameterType(replicationParameter.name, "" + replicationParameter.type, "" + value.GetType()));
            }           
        }

        //*** FullSyncRequestParameter ***//

        private static void InitFullSyncRequestParameters()
        {
            fullSyncRequestParameters = new Dictionary<FullSyncRequestParameter, Action<Query, object>>();
            FullSyncRequestParameter requestParameter;
            Action<Query, object> action;
            // DESCENDING
            requestParameter = FullSyncRequestParameter.DESCENDING;
            action = (query, value) => 
            {
                query.Descending = (bool) value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // ENDKEY
            requestParameter = FullSyncRequestParameter.ENDKEY;
            action = (query, value) =>
            {
                query.EndKey = value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // ENDKEY_DOCID
            requestParameter = FullSyncRequestParameter.ENDKEY_DOCID;
            action = (query, value) =>
            {
                query.EndKeyDocId = value as string;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // GROUP_LEVEL
            requestParameter = FullSyncRequestParameter.GROUP_LEVEL;
            action = (query, value) =>
            {
                query.GroupLevel = (int) value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // INCLUDE_DELETED
            requestParameter = FullSyncRequestParameter.INCLUDE_DELETED;
            action = (query, value) =>
            {
                query.IncludeDeleted = (bool) value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // INDEX_UPDATE_MODE
            requestParameter = FullSyncRequestParameter.INDEX_UPDATE_MODE;
            action = (query, value) =>
            {
                string valueStr = value as string;
                var indexUpdateModeValues = Enum.GetValues(typeof(IndexUpdateMode));
                var indexUpdateModeEnumerator = indexUpdateModeValues.GetEnumerator();

                while (indexUpdateModeEnumerator.MoveNext())
                {
                    var indexUpdateMode = (IndexUpdateMode) indexUpdateModeEnumerator.Current;
                    if (valueStr.Equals(indexUpdateMode.ToString()))
                    {
                        query.IndexUpdateMode = indexUpdateMode;
                        return;
                    }
                }
            };
            fullSyncRequestParameters[requestParameter] = action;
            // KEY
            requestParameter = FullSyncRequestParameter.KEY;
            action = (query, value) =>
            {
                query.Keys = new Object[] { value };
            };
            fullSyncRequestParameters[requestParameter] = action;
            // KEYS
            requestParameter = FullSyncRequestParameter.KEYS;
            action = (query, value) =>
            {
                query.Keys = value as IEnumerable<object>;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // LIMIT
            requestParameter = FullSyncRequestParameter.LIMIT;
            action = (query, value) =>
            {
                query.Limit = (int) value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // INCLUDE_DOCS
            requestParameter = FullSyncRequestParameter.INCLUDE_DOCS;
            action = (query, value) =>
            {
                query.Prefetch = (bool) value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // REDUCE
            requestParameter = FullSyncRequestParameter.REDUCE;
            action = (query, value) =>
            {
                query.MapOnly = ! (bool) value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // GROUP
            requestParameter = FullSyncRequestParameter.GROUP;
            action = (query, value) =>
            {
                query.GroupLevel = (bool) value ? 99 : 0;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // SKIP
            requestParameter = FullSyncRequestParameter.SKIP;
            action = (query, value) =>
            {
                query.Skip = (int) value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // STARTKEY
            requestParameter = FullSyncRequestParameter.STARTKEY;
            action = (query, value) =>
            {
                query.StartKey = value;
            };
            fullSyncRequestParameters[requestParameter] = action;
            // STARTKEY_DOCID
            requestParameter = FullSyncRequestParameter.STARTKEY_DOCID;
            action = (query, value) =>
            {
                query.StartKeyDocId = value as string;
            };
            fullSyncRequestParameters[requestParameter] = action;
        }

        //*** FullSyncPolicies ***//

        private static void InitFullSyncPolicies()
        {
            fullSyncPolicies = new Dictionary<FullSyncPolicy, Func<Database, IDictionary<string, object>, Document>>();
            FullSyncPolicy policy;
            Func<Database, IDictionary<string, object>, Document> func;
            // NONE
            policy = FullSyncPolicy.NONE;
            func = (database, newProperties) =>
            {
                Document createdDocument;
                try
                {
                    string documentId = C8oUtils.GetParameterStringValue(newProperties, C8oFullSync.FULL_SYNC__ID, false);

                    // removes special properties
                    newProperties.Remove(C8oFullSync.FULL_SYNC__ID);

                    // Creates a new document or get an existing one (if the ID is specified)
                    createdDocument = (documentId == null) ? database.CreateDocument() : database.GetDocument(documentId);
                    
                    createdDocument.PutProperties(newProperties);
                }
                catch (CouchbaseLiteException e)
                {
                    throw new C8oCouchbaseLiteException(C8oExceptionMessage.FullSyncPutProperties(newProperties), e);
                }
                return createdDocument;
            };
            fullSyncPolicies.Add(policy, func);
            // CREATE
            policy = FullSyncPolicy.CREATE;
            func = (database, newProperties) =>
            {
                Document createdDocument;
                try
                {
                    // Removes specials properties in order to create a new document
                    newProperties.Remove(C8oFullSync.FULL_SYNC__ID);
                    newProperties.Remove(C8oFullSync.FULL_SYNC__REV);
                    createdDocument = database.CreateDocument();
                    createdDocument.PutProperties(newProperties);
                }
                catch (CouchbaseLiteException e)
                {
                    throw new C8oCouchbaseLiteException(C8oExceptionMessage.FullSyncPutProperties(newProperties), e);
                }
                return createdDocument;
            };
            fullSyncPolicies.Add(policy, func);
            // OVERRIDE
            policy = FullSyncPolicy.OVERRIDE;
            func = (database, newProperties) =>
            {
                Document createdDocument;
                try
                {
                    // Gets the document ID
                    string documentId = C8oUtils.GetParameterStringValue(newProperties, C8oFullSync.FULL_SYNC__ID, false);

                    // Removes special properties in order to create a new document
                    newProperties.Remove(C8oFullSync.FULL_SYNC__ID);
                    newProperties.Remove(C8oFullSync.FULL_SYNC__REV);

                    // Creates a new document or get an existing one (if the ID is specified)
                    if (documentId == null)
                    {
                        createdDocument = database.CreateDocument();
                    }
                    else
                    {
                        createdDocument = database.GetDocument(documentId);
                        // Must add the current revision to the properties
                        var currentRevision = createdDocument.CurrentRevision;
                        if (currentRevision != null)
                        {
                            newProperties[C8oFullSync.FULL_SYNC__REV] = currentRevision.Id;
                        }
                    }

                    createdDocument.PutProperties(newProperties);
                }
                catch (CouchbaseLiteException e)
                {
                    throw new C8oCouchbaseLiteException(C8oExceptionMessage.FullSyncPutProperties(newProperties), e);
                }
                return createdDocument;
            };
            fullSyncPolicies.Add(policy, func);
            // MERGE
            policy = FullSyncPolicy.MERGE;
            func = (database, newProperties) =>
            {
                Document createdDocument;
                try
                {
                    // Gets the document ID
                    string documentId = C8oUtils.GetParameterStringValue(newProperties, C8oFullSync.FULL_SYNC__ID, false);

                    // Removes special properties in order to create a new document
                    newProperties.Remove(C8oFullSync.FULL_SYNC__ID);
                    newProperties.Remove(C8oFullSync.FULL_SYNC__REV);

                    // Creates a new document or get an existing one (if the ID is specified)
                    if (documentId == null)
                    {
                        createdDocument = database.CreateDocument();
                    }
                    else
                    {
                        createdDocument = database.GetDocument(documentId);
                    }

                    // Merges old properties with the new ones
                    var oldProperties = createdDocument.Properties;
                    if (oldProperties != null)
                    {
                        FullSyncUtils.MergeProperties(newProperties, oldProperties);
                    }

                    createdDocument.PutProperties(newProperties);
                }
                catch (CouchbaseLiteException e)
                {
                    throw new C8oCouchbaseLiteException(C8oExceptionMessage.FullSyncPutProperties(newProperties), e);
                }
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
                replication.DocIds = value as IEnumerable<string>;
            };
            fullSyncReplicationParameters.Add(replicationParameter, action);
            // LIVE
            replicationParameter = FullSyncReplicationParameter.LIVE;
            action = (replication, value) =>
            {
                replication.Continuous = (bool) value;
            };
            fullSyncReplicationParameters.Add(replicationParameter, action);
        }
    }
}
