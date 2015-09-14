using Convertigo.SDK.Listeners;
using Convertigo.SDK.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.FullSync.Enums
{
    //*** FullSync requestables ***//

    /// <summary>
    /// FullSync requestables.
    /// </summary>
    public class FullSyncRequestable
    {
        public static readonly FullSyncRequestable GET = new FullSyncRequestable("get", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            String docidParameterValue = C8oUtils.PeekParameterStringValue(parameters, FullSyncGetDocumentParameter.DOCID.name, true);
            return fullSyncInterface.HandleGetDocumentRequest(fullSyncDatabase, docidParameterValue, parameters);
        });

        public static readonly FullSyncRequestable DELETE = new FullSyncRequestable("delete", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            String docidParameterValue = C8oUtils.PeekParameterStringValue(parameters, FullSyncGetDocumentParameter.DOCID.name, true);
            return fullSyncInterface.handleDeleteDocumentRequest(fullSyncDatabase, docidParameterValue, parameters);
        });

        public static readonly FullSyncRequestable POST = new FullSyncRequestable("post", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            // Gets the policy parameter
            String fullSyncPolicyParameter = C8oUtils.PeekParameterStringValue(parameters, FullSyncPostDocumentParameter.POLICY.name, false);

            // Finds the policy corresponding to the parameter value if it exists
            FullSyncPolicy fullSyncPolicy = FullSyncPolicy.GetFullSyncPolicy(fullSyncPolicyParameter);

            return fullSyncInterface.handlePostDocumentRequest(fullSyncDatabase, fullSyncPolicy, parameters);
        });

        public static readonly FullSyncRequestable ALL = new FullSyncRequestable("all", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            return fullSyncInterface.HandleAllDocumentsRequest(fullSyncDatabase, parameters);
        });

        public static readonly FullSyncRequestable VIEW = new FullSyncRequestable("view", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            // Gets the design doc parameter value
            String ddocParameterValue = C8oUtils.PeekParameterStringValue(parameters, FullSyncGetViewParameter.DDOC.name, false);
            // Gets the view name parameter value
            String viewParameterValue = C8oUtils.PeekParameterStringValue(parameters, FullSyncGetViewParameter.VIEW.name, false);

            return fullSyncInterface.HandleGetViewRequest(fullSyncDatabase, ddocParameterValue, viewParameterValue, parameters);
        });

        public static readonly FullSyncRequestable SYNC = new FullSyncRequestable("sync", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            return fullSyncInterface.HandleSyncRequest(fullSyncDatabase, parameters, c8oResponseListener);
        });

        public static readonly FullSyncRequestable REPLICATE_PULL = new FullSyncRequestable("replicate_pull", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            return fullSyncInterface.HandleReplicatePullRequest(fullSyncDatabase, parameters, c8oResponseListener);
        });

        public static readonly FullSyncRequestable REPLICATE_PUSH = new FullSyncRequestable("replicate_push", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            return fullSyncInterface.HandleReplicatePushRequest(fullSyncDatabase, parameters, c8oResponseListener);
        });

        public static readonly FullSyncRequestable RESET = new FullSyncRequestable("reset", (fullSyncInterface, fullSyncDatabase, parameters, c8oResponseListener) =>
        {
            return fullSyncInterface.HandleResetDatabaseRequest(fullSyncDatabase);
        });

        public readonly String value;
        private readonly Func<FullSyncInterface, String, Dictionary<String, Object>, C8oResponseListener, Object> handleFullSyncrequestOp;

        private FullSyncRequestable(String value, Func<FullSyncInterface, String, Dictionary<String, Object>, C8oResponseListener, Object> handleFullSyncrequestOp)
        {
            this.value = value;
            this.handleFullSyncrequestOp = handleFullSyncrequestOp;
        }

        internal Object HandleFullSyncRequest(FullSyncInterface fullSyncInterface, String fullSyncDatabaseName, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListner)
        {
            return this.handleFullSyncrequestOp(fullSyncInterface, fullSyncDatabaseName, parameters, c8oResponseListner);
        }

        internal static FullSyncRequestable GetFullSyncRequestable(String value)
        {
            FullSyncRequestable[] fullSyncRequestableValues = FullSyncRequestable.Values();
            foreach (FullSyncRequestable fullSyncRequestable in fullSyncRequestableValues)
            {
                if (fullSyncRequestable.value.Equals(value))
                {
                    return fullSyncRequestable;
                }
            }
            return null;
        }

        public static FullSyncRequestable[] Values()
        {
            return new FullSyncRequestable[] { GET, DELETE, POST, ALL, VIEW, SYNC, REPLICATE_PULL, REPLICATE_PUSH, RESET };
        }
    }

    //*** Common parameters ***//

    /// <summary>
    /// Parameters common to some fullSync's requests.
    /// </summary>
    public class FullSyncRequestParameter
    {
        public static readonly FullSyncRequestParameter DESCENDING = new FullSyncRequestParameter("descending", typeof(Boolean));
        public static readonly FullSyncRequestParameter ENDKEY = new FullSyncRequestParameter("endkey", typeof(Object));
        public static readonly FullSyncRequestParameter ENDKEY_DOCID = new FullSyncRequestParameter("endkey_docid", typeof(String));
        public static readonly FullSyncRequestParameter GROUP_LEVEL = new FullSyncRequestParameter("group_level", typeof(int));
        public static readonly FullSyncRequestParameter INCLUDE_DELETED = new FullSyncRequestParameter("include_deleted", typeof(Boolean));
        public static readonly FullSyncRequestParameter INDEX_UPDATE_MODE = new FullSyncRequestParameter("index_update_mode", typeof(String));
        public static readonly FullSyncRequestParameter KEYS = new FullSyncRequestParameter("keys", typeof(IEnumerable<Object>));
        public static readonly FullSyncRequestParameter LIMIT = new FullSyncRequestParameter("limit", typeof(int));
        public static readonly FullSyncRequestParameter MAP_ONLY = new FullSyncRequestParameter("map_only", typeof(Boolean));
        public static readonly FullSyncRequestParameter PREFETCH = new FullSyncRequestParameter("prefetch", typeof(Boolean));
        public static readonly FullSyncRequestParameter SKIP = new FullSyncRequestParameter("skip", typeof(int));
        public static readonly FullSyncRequestParameter STARTKEY = new FullSyncRequestParameter("startkey", typeof(Object));
        public static readonly FullSyncRequestParameter STARTKEY_DOCID = new FullSyncRequestParameter("startkey_docid", typeof(String));

        public readonly String name;
        public readonly Type type;

        private FullSyncRequestParameter(String name, Type type)
        {
            this.name = name; 
            this.type = type;
        }

        public static FullSyncRequestParameter[] Values()
        {
            return new FullSyncRequestParameter[] { DESCENDING, ENDKEY, ENDKEY_DOCID, GROUP_LEVEL, INCLUDE_DELETED, INDEX_UPDATE_MODE, KEYS, LIMIT, MAP_ONLY, PREFETCH, SKIP, STARTKEY, STARTKEY_DOCID };
        }

        public static FullSyncRequestParameter GetFullSyncRequestParameter(String name)
        {
            if (name != null) 
            {
                foreach (FullSyncRequestParameter fullSyncRequestParameter in FullSyncRequestParameter.Values())
                {
                    if (name.Equals(fullSyncRequestParameter.name))
                    {
                        return fullSyncRequestParameter;
                    }
                }
            }
            return null;
        }

    }


    //*** Specific parameters ***//

    public class FullSyncGetViewParameter {
        public static readonly FullSyncGetViewParameter VIEW = new FullSyncGetViewParameter("view");
		public static readonly FullSyncGetViewParameter DDOC = new FullSyncGetViewParameter("ddoc");

		public readonly String name;
		
		private FullSyncGetViewParameter(String name) 
        {
			this.name = name;
		}
	}

    public class FullSyncGetDocumentParameter
    {
        public static readonly FullSyncGetDocumentParameter DOCID = new FullSyncGetDocumentParameter("docid");

        public readonly String name;

        private FullSyncGetDocumentParameter(String name)
        {
            this.name = name;
        }

    }

    public class FullSyncDeleteDocumentParameter
    {
        public static readonly FullSyncDeleteDocumentParameter DOCID = new FullSyncDeleteDocumentParameter("docid");
        public static readonly FullSyncDeleteDocumentParameter REV = new FullSyncDeleteDocumentParameter("rev");

        public readonly String name;

        private FullSyncDeleteDocumentParameter(String name)
        {
            this.name = name;
        }
    }

    public class FullSyncPostDocumentParameter 
    {
        public static readonly FullSyncPostDocumentParameter POLICY = new FullSyncPostDocumentParameter("_use_policy");
        public static readonly FullSyncPostDocumentParameter SUBKEY_SEPARATOR = new FullSyncPostDocumentParameter("_use_subkey_separator");
		
		public readonly String name;
		
		private FullSyncPostDocumentParameter(String name) {
			this.name = name;
		}

        public static FullSyncPostDocumentParameter[] Values()
        {
            return new FullSyncPostDocumentParameter[] { POLICY, SUBKEY_SEPARATOR };
        }

	}

    /// <summary>
    /// Specific parameters for the fullSync's replicateDatabase request (push or pull).
    /// </summary>
    public class FullSyncReplicationParameter
    {
        public static readonly FullSyncReplicationParameter CANCEL = new FullSyncReplicationParameter("cancel", typeof(Object));
        public static readonly FullSyncReplicationParameter LIVE = new FullSyncReplicationParameter("live", typeof(Boolean));
        public static readonly FullSyncReplicationParameter DOCIDS = new FullSyncReplicationParameter("docids", typeof(IEnumerable<String>));

        public readonly String name;
        public readonly Type type;

        private FullSyncReplicationParameter(String name, Type type) 
        {
            this.name = name;
            this.type = type;
        }

        public static FullSyncReplicationParameter[] Values()
        {
            return new FullSyncReplicationParameter[] { CANCEL, LIVE, DOCIDS };
        }
    }

    //*** Policy ***//

    /// <summary>
    /// The policies of the fullSync's postDocument request. 
    /// </summary>
    public class FullSyncPolicy
    {
        public static readonly FullSyncPolicy NONE = new FullSyncPolicy("none");
        public static readonly FullSyncPolicy CREATE = new FullSyncPolicy("create");
        public static readonly FullSyncPolicy OVERRIDE = new FullSyncPolicy("override");
        public static readonly FullSyncPolicy MERGE = new FullSyncPolicy("merge");

        public readonly String value;

        private FullSyncPolicy(String value)
        {
            this.value = value;
        }

        public static FullSyncPolicy[] Values()
        {
            return new FullSyncPolicy[] { NONE, CREATE, OVERRIDE, MERGE };
        }

        public static FullSyncPolicy GetFullSyncPolicy(String value)
        {
            if (value != null)
            {
                FullSyncPolicy[] fullSyncPolicyValues = FullSyncPolicy.Values();
                foreach (FullSyncPolicy fullSyncPolicy in fullSyncPolicyValues)
                {
                    if (fullSyncPolicy.value.Equals(value))
                    {
                        return fullSyncPolicy;
                    }
                }
            }
            return NONE;
        }
    }


}
