using Convertigo.SDK.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

// TODO
// doc
// log
// trust all certificates
// exception
// certificate client et serveur

/// <summary>
/// This is the namespace for Convertigo SDK
/// </summary>
namespace Convertigo.SDK
{
    /// <summary>
    /// Allows to send requests to a Convertigo Server (or Studio), these requests are called c8o calls.<para/>
    /// C8o calls are done thanks to a HTTP request or a CouchbaseLite usage.<para/>
    /// An instance of C8o is connected to only one Convertigo and can't change it.<para/>
    /// To use it, you have to first initialize the C8o instance with the Convertigo endpoint, then use call methods with Convertigo variables as parameter.
    /// </summary>
    public class C8o : C8oBase
    {
        //*** Regular Expression ***//

        /// <summary>
        /// The regex used to handle the c8o requestable syntax ("<project>.<sequence>" or "<project>.<connector>.<transaction>")
        /// </summary>
        private static readonly Regex RE_REQUESTABLE = new Regex(@"^([^.]*)\.(?:([^.]+)|([^.]+)\.([^.]+))$", RegexOptions.IgnoreCase);
        /// <summary>
        /// The regex used to get the part of the endpoint before '/projects/'
        /// </summary>
        private static readonly Regex RE_ENDPOINT = new Regex(@"^(http(s)?://([^:]+)(:[0-9]+)?/?.*?)/projects/([^/]+)$", RegexOptions.IgnoreCase);

        //*** Engine reserved parameters ***//

        internal static readonly string ENGINE_PARAMETER_PROJECT = "__project";
        internal static readonly string ENGINE_PARAMETER_SEQUENCE = "__sequence";
        internal static readonly string ENGINE_PARAMETER_CONNECTOR = "__connector";
        internal static readonly string ENGINE_PARAMETER_TRANSACTION = "__transaction";
        internal static readonly string ENGINE_PARAMETER_ENCODED = "__encoded";
        internal static readonly string ENGINE_PARAMETER_DEVICE_UUID = "__uuid";
        internal static readonly string ENGINE_PARAMETER_PROGRESS = "__progress";
        internal static readonly string ENGINE_PARAMETER_FROM_LIVE = "__fromLive";

        //*** FULLSYNC parameters ***//
        /// <summary>
        /// Constant to use as a parameter for a Call of "fs://.post" and must be followed by a FS_POLICY_* constant.
        /// </summary>
        /// <example>
        /// <code>
        /// c8o.CallJson("fs://.post",
        ///   C8o.FS_POLICY, C8O.FS_POLICY_MERGE,
        ///   "docid", myid,
        ///   "mykey", myvalue
        /// ).Sync();
        /// </code>
        /// </example>
        public static readonly string FS_POLICY = "_use_policy";
        /// <summary>
        /// Use it with "fs://.post" and C8o.FS_POLICY.<para/>
        /// This is the default post policy that don't alter the document before the CouchbaseLite's insertion.
        /// </summary>
        public static readonly string FS_POLICY_NONE = "none";
        /// <summary>
        /// Use it with "fs://.post" and C8o.FS_POLICY.<para/>
        /// This post policy remove the "_id" and "_rev" of the document before the CouchbaseLite's insertion.
        /// </summary>
        public static readonly string FS_POLICY_CREATE = "create";
        /// <summary>
        /// Use it with "fs://.post" and C8o.FS_POLICY.<para/>
        /// This post policy inserts the document in CouchbaseLite even if a document with the same "_id" already exists.
        /// </summary>
        public static readonly string FS_POLICY_OVERRIDE = "override";
        /// <summary>
        /// Use it with "fs://.post" and C8o.FS_POLICY.<para/>
        /// This post policy merge the document with an existing document with the same "_id" before the CouchbaseLite's insertion.
        /// </summary>
        public static readonly string FS_POLICY_MERGE = "merge";
        /// <summary>
        /// Use it with "fs://.post". Default value is ".".<para/>
        /// This key allow to override the sub key separator in case of document depth modification.
        /// </summary>
        public static readonly string FS_SUBKEY_SEPARATOR = "_use_subkey_separator";

        /// <summary>
        /// Use it with "fs://.post". Default value is ".".<para/>
        /// This key allow to override the sub key separator in case of document depth modification.
        /// </summary>
        public static readonly string FS_STORAGE_SQL = "SQL";
        /// <summary>
        /// Use it with "c8oSettings.setFullSyncStorageEngine" to choose the SQL fullsync storage engine.
        /// </summary>
        public static readonly string FS_STORAGE_FORESTDB = "FORESTDB";
        /// <summary>
        /// Use it with "fs://" request as parameter to enable the live request feature.<br/>
        /// Must be followed by a string parameter, the 'liveid' that can be use to cancel the live
        /// request using c8o.cancelLive(liveid) method.<br/>
        /// A live request automatically recall the then or thenUI handler when the database changed.
        /// </summary>
        public static readonly string FS_LIVE = "__live";

        //*** Local cache keys ***//

        internal static readonly string LOCAL_CACHE_DOCUMENT_KEY_RESPONSE = "response";
        internal static readonly string LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE = "responseType";
        internal static readonly string LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE = "expirationDate";

        internal static readonly string LOCAL_CACHE_DATABASE_NAME = "c8olocalcache";

	    //*** Response type ***//

        internal static readonly string RESPONSE_TYPE_XML = "pxml";
        internal static readonly string RESPONSE_TYPE_JSON = "json";

        //*** Static configuration ***//
        internal static Type C8oHttpInterfaceUsed;
        internal static Type C8oFullSyncUsed;
        internal static Func<bool> defaultIsUi;
        internal static Action<Action> defaultUiDispatcher;
        internal static Action<Action> defaultBgDispatcher;
        internal static string deviceUUID;

        /// <summary>
        /// Returns the current version of the SDK as "x.y.z".
        /// </summary>
        /// <returns>Current version of the SDK as "x.y.z".</returns>
        public static string GetSdkVersion()
        {
            return "2.1.1";
        }

        //*** Attributes ***//

        /// <summary>
        /// The Convertigo endpoint, syntax : <protocol>://<server>:<port>/<Convertigo web app path>/projects/<project name> (Example : http://127.0.0.1:18080/convertigo/projects/MyProject)
        /// </summary>
        private string endpoint;
        private string endpointConvertigo;
        private bool endpointIsSecure;
        private string endpointHost;
        private string endpointPort;
        private string endpointProject;


        /// <summary>
        /// Used to run HTTP requests.
        /// </summary>
        internal C8oHttpInterface httpInterface;

        /// <summary>
        /// Allows to log locally and remotely to the Convertigo server.
        /// </summary>
        internal C8oLogger c8oLogger;

        /// <summary>
        /// Allows to make fullSync calls.
        /// </summary>
        internal C8oFullSync c8oFullSync;

        internal IDictionary<string, C8oCallTask> lives = new Dictionary<string, C8oCallTask>();
        internal IDictionary<string, string> livesDb = new Dictionary<string, string>();

        //*** Constructors ***//

        /// <summary>
        /// This is the base object representing a Convertigo Server end point. This object should be instanciated
        /// when the apps starts and be accessible from any class of the app. Although this is not common, you may have
        /// several C8o objects instantiated in your app.    
        /// </summary>
        /// <param name="endpoint">The Convertigo endpoint, syntax : &lt;protocol&gt;://&lt;server&gt;:&lt;port&gt;/&lt;Convertigo web app path&gt;/projects/&lt;project name&gt;<para/>
        /// Example : http://computerName:18080/convertigo/projects/MyProject
        /// </param>
        /// <param name="c8oSettings">Initialization options.<para/>
        /// Example : new C8oSettings().setLogRemote(false).setDefaultDatabaseName("sample")
        /// </param>
        public C8o(string endpoint, C8oSettings c8oSettings = null)
        {
            // Checks the URL validity
            if (!C8oUtils.IsValidUrl(endpoint))
            {
                throw new System.ArgumentException(C8oExceptionMessage.InvalidArgumentInvalidURL(endpoint));
            }

            // Checks the endpoint validty
            var matches = RE_ENDPOINT.Match(endpoint);
            if (!matches.Success)
            {
                throw new System.ArgumentException(C8oExceptionMessage.InvalidArgumentInvalidEndpoint(endpoint));
            }

            this.endpoint = endpoint;

            endpointConvertigo = matches.Groups[1].Value;
            endpointIsSecure = matches.Groups[2].Value != null;
            endpointHost = matches.Groups[3].Value;
            endpointPort = matches.Groups[4].Value;
            endpointProject = matches.Groups[5].Value;

            if (c8oSettings != null)
            {
                Copy(c8oSettings);
            }

            if (uiDispatcher == null)
            {
                uiDispatcher = defaultUiDispatcher;
            }

            try
            {
                if (C8oHttpInterfaceUsed != null)
                {
                    httpInterface = C8oHttpInterfaceUsed.GetTypeInfo().DeclaredConstructors.ElementAt(1).Invoke(new object[] { this }) as C8oHttpInterface;
                }
                else
                {
                    httpInterface = new C8oHttpInterface(this);
                }
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.InitHttpInterface(), e);
            }

            c8oLogger = new C8oLogger(this);

            c8oLogger.LogMethodCall("C8o constructor");

            try
            {
                if (C8oFullSyncUsed != null)
                {
                    c8oFullSync = C8oFullSyncUsed.GetTypeInfo().DeclaredConstructors.ElementAt(0).Invoke(new object[0]) as C8oFullSync;
                    c8oFullSync.Init(this);
                }
                else
                {
                    c8oFullSync = null; // new C8oFullSyncHttp(FullSyncServerUrl, FullSyncUsername, FullSyncPassword);
                }
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.FullSyncInterfaceInstance(), e);
            }
        }

        //*** C8o calls ***//
        public void Call(string requestable, IDictionary<string, object> parameters = null, C8oResponseListener c8oResponseListener = null, C8oExceptionListener c8oExceptionListener = null)
        {
            try
            {
                if (requestable == null)
                {
                    throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("requestable"));
                }

                // Checks parameters validity
                if (parameters == null)
                {
                    parameters = new Dictionary<string, object>();
                }
                else
                {
                    // Clone parameters in order to modify them
                    parameters = new Dictionary<string, object>(parameters);
                }

                // Use the requestable string to add parameters corresponding to the c8o project, sequence, connector and transaction (<project>.<sequence> or <project>.<connector>.<transaction>)
                var matches = RE_REQUESTABLE.Match(requestable);
                if (!matches.Success)
                {
                    // The requestable is not correct so the default transaction of the default connector will be called
                    throw new System.ArgumentException(C8oExceptionMessage.InvalidRequestable(requestable));
                }

                // If the project name is specified
                if (matches.Groups[1].Value != "")
                {
                    parameters[ENGINE_PARAMETER_PROJECT] = matches.Groups[1].Value;
                }
                // If the C8o call use a sequence
                if (matches.Groups[2].Value != "")
                {
                    parameters[ENGINE_PARAMETER_SEQUENCE] = matches.Groups[2].Value;
                }
                else
                {
                    parameters[ENGINE_PARAMETER_CONNECTOR] = matches.Groups[3].Value;
                    parameters[ENGINE_PARAMETER_TRANSACTION] = matches.Groups[4].Value;
                }

                Call(parameters, c8oResponseListener, c8oExceptionListener);
            }
            catch (Exception e)
            {
                HandleCallException(c8oExceptionListener, parameters, e);
            }
        }

        public void Call(IDictionary<string, object> parameters = null, C8oResponseListener c8oResponseListener = null, C8oExceptionListener c8oExceptionListener = null)
        {
            // IMPORTANT : all c8o calls have to end here !
            try
            {
                c8oLogger.LogMethodCall("Call", parameters);

                // Checks parameters validity
                if (parameters == null)
                {
                    parameters = new Dictionary<string, object>();
                }
                else
                {
                    // Clones parameters in order to modify them
                    parameters = new Dictionary<string, object>(parameters);
                }

                // Creates a async task running on another thread
                // Exceptions have to be handled by the C8oExceptionListener
                var task = new C8oCallTask(this, parameters, c8oResponseListener, c8oExceptionListener);
                task.Execute();
            }
            catch (Exception e)
            {
                HandleCallException(c8oExceptionListener, parameters, e);
            }
        }


        /// <summary>
        /// Call a Convertigo Server backend service and return data in a JSON Object.
        /// CallJSON will asynchrously call a "requestable" (Sequence, transaction or FullSync database) and return a
        /// C8oPromise object.
        /// </summary>
        /// <param name="requestable">
        /// A "requestable" object of this form :
        /// <list type ="bullet">
        ///     <item>project.sequence to call a Sequence in the convertigo server. If project is not specified explicitly here, 
        ///     (.sequence) the default project specified in the enpoint will be used.</item>
        ///     <item>
        ///     project.connector.transaction to call a transaction in the convertigo server. if project is not specified explicitly here, 
        ///     (.connector.transaction) the default project specified in the enpoint will be used. If
        ///     connector is not specified (..transaction) the default connector will be used.</item>
        ///     <item>fs://database.fullsync_verb   to call the local NoSQL database for quering, updating and syncing according to the full_sync
        ///     verb used. See FullSync documentation for a list of verbs and parameters.</item>
        /// </list>
        /// </param>
        /// <param name="parameters">
        /// A IDictionary of Key/Value pairs mapped on Sequence/transaction/fullsync variables.
        /// </param>
        /// <returns>
        /// A C8oPromise object on which you can chain other requests to get the data with the Then(), ThenUI() methods or
        /// use the Async() to wait for the server response without blocking the request thread. You can also use the .Fail() and
        /// FailUI() methods to handle errors.
        /// </returns>
        public C8oPromise<JObject> CallJson(string requestable, IDictionary<string, object> parameters)
        {
            var promise = new C8oPromise<JObject>(this);

            Call(requestable, parameters, new C8oResponseJsonListener((response, requestParameters) =>
            {
                if (response == null && requestParameters.ContainsKey(ENGINE_PARAMETER_PROGRESS))
                {
                    promise.OnProgress(requestParameters[ENGINE_PARAMETER_PROGRESS] as C8oProgress);
                }
                else
                {
                    promise.OnResponse(response, requestParameters);
                }
            }), new C8oExceptionListener((exception, requestParameters) =>
            {
                promise.OnFailure(exception, requestParameters);
            }));

            return promise;
        }

        /// <summary>
        /// Call a Convertigo Server backend service and return data in a JSON Object.
        /// CallJSON will asynchrously call a "requestable" (Sequence, transaction or FullSync database) and return a
        /// C8oPromise object.
        /// </summary>
        /// <param name="requestable">
        /// A "requestable" object of this form :
        /// <list type ="bullet">
        ///     <item>project.sequence to call a Sequence in the convertigo server. If project is not specified explicitly here, 
        ///     (.sequence) the default project specified in the enpoint will be used.</item>
        ///     <item>
        ///     project.connector.transaction to call a transaction in the convertigo server. if project is not specified explicitly here, 
        ///     (.connector.transaction) the default project specified in the enpoint will be used. If
        ///     connector is not specified (..transaction) the default connector will be used.</item>
        ///     <item>fs://database.fullsync_verb   to call the local NoSQL database for quering, updating and syncing according to the full_sync
        ///     verb used. See FullSync documentation for a list of verbs and parameters.</item>
        /// </list>
        /// </param>
        /// <param name="parameters">
        /// A a list of Key/Value pairs mapped on Sequence/transaction/fullsync variables.
        /// </param>
        /// <returns>
        /// A C8oPromise object on which you can chain other requests to get the data with the Then(), ThenUI() methods or
        /// use the Async() to wait for the server response without blocking the request thread. You can also use the .Fail() and
        /// FailUI() methods to handle errors.
        /// </returns>
        /// <sample>
        /// This is a sample usage of CallJSON to call a "select_shop" sequence providing a shopCode variable ste to "42". We use the
        /// Async() method to wait without blocking the calling thread with the await operator.
        /// <code>
        ///     JObject data = await myC8o.CallJSON(".select_shop", "shopCode, "42").Async();
        /// </code>
        /// or this code to use the promise :
        /// <code>
        ///    myC8o.CallJson (".select_shop",							 // This is the requestable
		///        "shopCode", "42"										 // The key/value parameters to the sequence
	    ///    ).Then((response, parameters) => {						 // This will run as soon as the Convertigo server responds
		///        // do my stuff in a	 worker thread					 // This is worker thread not suitable to update UI
		///        String sc = (String)response["document"]["shopCode"]; // Get the data using Linq
        ///        myC8o.Log (C8oLogLevel.DEBUG, sc);					 // Log data on the Convertigo Server
		///        return null;											 // last step of the promise chain, return null
	    ///    });
        /// </code>
        /// </sample>
        public C8oPromise<JObject> CallJson(string requestable, params object[] parameters)
        {
            return CallJson(requestable, ToParameters(parameters));
        }

        /// <summary>
        /// Call a Convertigo Server backend service and return data in a JSON Object.
        /// CallJSON will asynchrously call a "requestable" (Sequence, transaction or FullSync database) and return a
        /// C8oPromise object.
        /// </summary>
        /// <param name="requestable">
        /// A "requestable" object of this form :
        /// <list type ="bullet">
        ///     <item>project.sequence to call a Sequence in the convertigo server. If project is not specified explicitly here, 
        ///     (.sequence) the default project specified in the enpoint will be used.</item>
        ///     <item>
        ///     project.connector.transaction to call a transaction in the convertigo server. if project is not specified explicitly here, 
        ///     (.connector.transaction) the default project specified in the enpoint will be used. If
        ///     connector is not specified (..transaction) the default connector will be used.</item>
        ///     <item>fs://database.fullsync_verb   to call the local NoSQL database for quering, updating and syncing according to the full_sync
        ///     verb used. See FullSync documentation for a list of verbs and parameters.</item>
        /// </list>
        /// </param>
        /// <param name="parameters">
        /// A JObject of Key/Value pairs mapped on Sequence/transaction/fullsync variables.
        /// </param>
        /// <returns>
        /// A C8oPromise object on which you can chain other requests to get the data with the Then(), ThenUI() methods or
        /// use the Async() to wait for the server response without blocking the request thread. You can also use the .Fail() and
        /// FailUI() methods to handle errors.
        /// </returns>
        /// <sample>
        /// This is a sample usage of CallJSON to call a "select_shop" sequence providing a shopCode variable ste to "42". We use the
        /// Async() method to wait without blocking the calling thread with the await operator.
        /// <code>
        ///     JObject parameter = new JObject() {{"shopCode", "42"}};
        ///     JObject data = await myC8o.CallJSON(".select_shop", parameter).Async();
        /// </code>
        /// or this code to use the promise :
        /// <code>
        ///    myC8o.CallJson (".select_shop",							 // This is the requestable
		///        parameter	    									 // The key/value parameters to the sequence
	    ///    ).Then((response, parameters) => {						 // This will run as soon as the Convertigo server responds
		///        // do my stuff in a	 worker thread					 // This is worker thread not suitable to update UI
		///        String sc = (String)response["document"]["shopCode"]; // Get the data using Linq
        ///        myC8o.Log (C8oLogLevel.DEBUG, sc);					 // Log data on the Convertigo Server
		///        return null;											 // last step of the promise chain, return null
	    ///    });
        /// </code>
        /// </sample>
        public C8oPromise<JObject> CallJson(string requestable, JObject parameters)
        {
            return CallJson(requestable, parameters.ToObject<IDictionary<string, object>>());
        }


        /// <summary>
        /// Call a Convertigo Server backend service and return data as an XML Document.
        /// CallXML will asynchrously call a "requestable" (Sequence, transaction or FullSync database) and return a
        /// C8oPromise object.
        /// </summary>
        /// <param name="requestable">
        /// A "requestable" object of this form :
        /// <list type ="bullet">
        ///     <item>project.sequence to call a Sequence in the convertigo server. If project is not specified explicitly here, 
        ///     (.sequence) the default project specified in the enpoint will be used.</item>
        ///     <item>
        ///     project.connector.transaction to call a transaction in the convertigo server. if project is not specified explicitly here, 
        ///     (.connector.transaction) the default project specified in the enpoint will be used. If
        ///     connector is not specified (..transaction) the default connector will be used.</item>
        ///     <item>fs://database.fullsync_verb   to call the local NoSQL database for quering, updating and syncing according to the full_sync
        ///     verb used. See FullSync documentation for a list of verbs and parameters.</item>
        /// </list>
        /// </param>
        /// <param name="parameters">
        /// A IDictionary of Key/Value pairs mapped on Sequence/transaction/fullsync variables.
        /// </param>
        /// <returns>
        /// A C8oPromise object on which you can chain other requests to get the data with the Then(), ThenUI() methods or
        /// use the Async() to wait for the server response without blocking the request thread. You can also use the .Fail() and
        /// FailUI() methods to handle errors.
        /// </returns>
        public C8oPromise<XDocument> CallXml(string requestable, IDictionary<string, object> parameters)
        {
            var promise = new C8oPromise<XDocument>(this);

            Call(requestable, parameters, new C8oResponseXmlListener((response, requestParameters) =>
            {
                if (response == null && requestParameters.ContainsKey(ENGINE_PARAMETER_PROGRESS))
                {
                    promise.OnProgress(requestParameters[ENGINE_PARAMETER_PROGRESS] as C8oProgress);
                }
                else
                {
                    promise.OnResponse(response, requestParameters);
                }
            }), new C8oExceptionListener((exception, requestParameters) =>
            {
                promise.OnFailure(exception, requestParameters);
            }));

            return promise;
        }


        /// <summary>
        /// Call a Convertigo Server backend service and return data as an XML Document.
        /// CallXML will asynchrously call a "requestable" (Sequence, transaction or FullSync database) and return a
        /// C8oPromise object.
        /// </summary>
        /// <param name="requestable">
        /// A "requestable" object of this form :
        /// <list type ="bullet">
        ///     <item>project.sequence to call a Sequence in the convertigo server. If project is not specified explicitly here, 
        ///     (.sequence) the default project specified in the enpoint will be used.</item>
        ///     <item>
        ///     project.connector.transaction to call a transaction in the convertigo server. if project is not specified explicitly here, 
        ///     (.connector.transaction) the default project specified in the enpoint will be used. If
        ///     connector is not specified (..transaction) the default connector will be used.</item>
        ///     <item>fs://database.fullsync_verb   to call the local NoSQL database for quering, updating and syncing according to the full_sync
        ///     verb used. See FullSync documentation for a list of verbs and parameters.</item>
        /// </list>
        /// </param>
        /// <param name="parameters">
        /// A a list of Key/Value pairs mapped on Sequence/transaction/fullsync variables.
        /// </param>
        /// <returns>
        /// A C8oPromise object on which you can chain other requests to get the data with the Then(), ThenUI() methods or
        /// use the Async() to wait for the server response without blocking the request thread. You can also use the .Fail() and
        /// FailUI() methods to handle errors.
        /// </returns>
        public C8oPromise<XDocument> CallXml(string requestable, params object[] parameters)
        {
            return CallXml(requestable, ToParameters(parameters));
        }

        /// <summary>
        /// You can use this method to add cookies to the HTTP interface. This can be very useful if you have to use some
        /// pre-initialized cookies coming from a global SSO Authentication for example.
        /// </summary>
        /// <param name="name">The cookie name</param>
        /// <param name="value">The cookie value</param>
        public void AddCookie(string name, string value)
        {
            httpInterface.AddCookie(name, value);
        }

        public new bool LogC8o
        {
            get { return logC8o; }
            set { logC8o = value; }
        }

        /// <summary>
        /// Gets a value indicating if logs are sent to the Convertigo server.
        /// </summary>
        /// <value>
        ///   <c>true</c> if logs are sent to the Convertigo server; otherwise, <c>false</c>.
        /// </value>
        public new bool LogRemote
        {
            get { return logRemote; }
            set { logRemote = value; }
        }
        
        /// <summary>
        /// Sets a value indicating the log level you want in the device console.
        /// </summary>
        /// <value>
        ///   <c>true</c> if logs are sent to the Convertigo server; otherwise, <c>false</c>.
        /// </value>
        public new C8oLogLevel LogLevelLocal
        {
            get { return logLevelLocal; }
            set { logLevelLocal = value; }
        }


        /// <summary>
        /// Set the storage engine for local FullSync databases. Use C8o.FS_STORAGE_SQL or C8o.FS_STORAGE_FORESTDB.
        /// </summary>
        public new string FullSyncStorageEngine
        {
            get { return fullSyncStorageEngine; }
            set
            {
                if (C8o.FS_STORAGE_SQL.Equals(value) ||
                        C8o.FS_STORAGE_FORESTDB.Equals(value))
                {
                    fullSyncStorageEngine = value;
                }
            }
        }

        /// <summary>
        /// Set the encryption key for local FullSync databases encryption.
        /// </summary>
        public new string FullSyncEncryptionKey
        {
            get { return fullSyncEncryptionKey; }
            set { fullSyncEncryptionKey = value; }
        }

        /// <summary>
        /// Logs a message to Convertigo Server. the message will be seen in Convertigo Server Device logger. Logging messages to the server
        /// helps in monitoring Mobile apps in production.
        /// </summary>
        public C8oLogger Log
        {
            get { return c8oLogger; }
        }

        public bool IsUI
        {
            get
            {
                if (defaultIsUi != null)
                {
                    return defaultIsUi();
                }
                return false;
            }
        }

        /// <summary>
        /// An utility method to run a worker thread on UI. This method is Cross-platform and works on all the supported
        /// platforms (iOS, Android, WPF)
        /// </summary>
        /// <param name="code">The code to run on the UI thread</param>
        public void RunUI(Action code)
        {
            if (UiDispatcher != null)
            {
                UiDispatcher.Invoke(code);
            }
            else
            {
                code.Invoke();
            }
        }

        /// <summary>
        /// Run a block of code in a background Thread.
        /// </summary>
        /// <param name="code">The code to run in the background Thread.</param>
        public void RunBG(Action code)
        {
            if (defaultBgDispatcher != null)
            {
                defaultBgDispatcher(code);
            }
            else
            {
                Task.Factory.StartNew(code, TaskCreationOptions.LongRunning);
            }
        }

        public override string ToString()
        {
            return "C8o[" + endpoint + "] " + base.ToString();
        }

        /// <summary>
        /// The endpoint of this C8O object as initialized in the constructor
        /// </summary>
        public string Endpoint
        {
            get { return endpoint; }
        }

        /// <summary>
        /// The name of the Convertigo server coming from the endpoint url.
        /// </summary>
        public string EndpointConvertigo
        {
            get { return endpointConvertigo; }
        }

        /// <summary>
        /// true if the endpoint has been initialized with a https:// url
        /// </summary>
        public bool EndpointIsSecure
        {
            get { return endpointIsSecure; }
        }

        /// <summary>
        /// The target server hostname, coming from the endpoint url.
        /// </summary>
        public string EndpointHost
        {
            get { return endpointHost; }
        }

        /// <summary>
        /// The target server hostname, coming from the endpoint url.
        /// </summary>
        public string EndpointPort
        {
            get { return endpointPort; }
        }

        /// <summary>
        /// The target project name, coming from the endpoint url.
        /// </summary>
        public string EndpointProject
        {
            get { return endpointProject; }
        }

        /// <summary>
        /// The Unique device ID for this mobile device. This value will be used in logs, analytics and billing tables
        /// The Device mode licence model is based on these unique devices Ids.
        /// </summary>
        public string DeviceUUID
        {
            get { return deviceUUID; }
        }

        /// <summary>
        /// The Cookie Store for this endpoint. All the cookies for this end point will be held here.
        /// </summary>
        public CookieContainer CookieStore
        {
            get { return httpInterface.CookieStore; }
        }

        /// <summary>
        /// Add a listener to monitor all changes of the 'db'.
        /// </summary>
        /// <param name="db">the name of the fullsync database to monitor. Use the default database for a blank or a null value.</param>
        /// <param name="listener">the listener to trigger on change.</param>
        public void AddFullSyncChangeListener(string db, C8oFullSyncChangeListener listener)
        {
            c8oFullSync.AddFullSyncChangeListener(db, listener);
        }

        /// <summary>
        /// Remove a listener for changes of the 'db'.
        /// </summary>
        /// <param name="db">the name of the fullsync database to monitor. Use the default database for a blank or a null value.</param>
        /// <param name="listener">the listener instance to remove.</param>
        public void RemoveFullSyncChangeListener(string db, C8oFullSyncChangeListener listener)
        {
            c8oFullSync.RemoveFullSyncChangeListener(db, listener);
        }

        internal void AddLive(string liveid, string db, C8oCallTask task)
        {
            CancelLive(liveid);
            lock (lives)
            {
                lives[liveid] = task;
            }

            lock (livesDb)
            {
                livesDb[liveid] = db;
            }

            AddFullSyncChangeListener(db, HandleFullSyncLive);
        }

        /// <summary>
        /// Cancel a live request previously enabled by the C8o.FS_LIVE parameter.
        /// </summary>
        /// <param name="liveid">The value associated with the C8o.FS_LIVE parameter.</param>
        public void CancelLive(string liveid)
        {
            if (livesDb.ContainsKey(liveid))
            {
                string db;
                lock (livesDb)
                {
                    db = livesDb[liveid];
                    livesDb.Remove(liveid);
                    if (livesDb.Values.Contains(db))
                    {
                        db = null;
                    }
                }

                if (db != null)
                {
                    RemoveFullSyncChangeListener(db, HandleFullSyncLive);
                }
            }
            lock (lives)
            {
                lives.Remove(liveid);
            }
        }

        private static IDictionary<string, object> ToParameters(object[] parameters)
        {
            if (parameters.Length % 2 != 0)
            {
                throw new System.ArgumentException(C8oExceptionMessage.InvalidParameterValue("parameters", "needs pairs of values"));
            }

            var newParameters = new Dictionary<string, object>();

            for (var i = 0; i < parameters.Length; i += 2)
            {
                newParameters["" + parameters[i]] = parameters[i + 1];
            }

            return newParameters;
        }

        internal void HandleCallException(C8oExceptionListener c8oExceptionListener, IDictionary<string, object> requestParameters, Exception exception)
        {
            c8oLogger._Warn("Handle a call exception", exception);

            if (c8oExceptionListener != null)
            {
                c8oExceptionListener.OnException(exception, requestParameters);
            }
        }

        internal void HandleFullSyncLive(JObject changes)
        {
            lock (lives)
            {
                foreach (var task in lives.Values)
                {
                    task.ExecuteFromLive();
                }
            }
        }
    }
}
