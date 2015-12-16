using Convertigo.SDK.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
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
    /// This base class for Convertigo SDK.  A C8o object represents a Convertigo MBaaS Server endpoint.
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
        private static readonly Regex RE_ENDPOINT = new Regex(@"^(http(s)?://([^:]+)(:[0-9]+)?/[^/]+)/projects/[^/]+$", RegexOptions.IgnoreCase);

        //*** Engine reserved parameters ***//

        public static readonly string ENGINE_PARAMETER_PROJECT = "__project";
        public static readonly string ENGINE_PARAMETER_SEQUENCE = "__sequence";
        public static readonly string ENGINE_PARAMETER_CONNECTOR = "__connector";
        public static readonly string ENGINE_PARAMETER_TRANSACTION = "__transaction";
        public static readonly string ENGINE_PARAMETER_ENCODED = "__encoded";
        public static readonly string ENGINE_PARAMETER_LOCAL_CACHE = "__localCache";
        public static readonly string ENGINE_PARAMETER_DEVICE_UUID = "__uuid";
        public static readonly string ENGINE_PARAMETER_PROGRESS = "__progress";

        //*** Local cache keys ***//

        public static readonly string LOCAL_CACHE_PARAMETER_KEY_ENABLED = "enabled";
        public static readonly string LOCAL_CACHE_PARAMETER_KEY_POLICY = "policy";
        public static readonly string LOCAL_CACHE_PARAMETER_KEY_TTL = "ttl";
        public static readonly string LOCAL_CACHE_DOCUMENT_KEY_RESPONSE = "response";
        public static readonly string LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE = "responseType";
        public static readonly string LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE = "expirationDate";

        public static readonly string LOCAL_CACHE_DATABASE_NAME = "c8olocalcache";

	    //*** Response type ***//

        public static readonly string RESPONSE_TYPE_XML = "pxml";
        public static readonly string RESPONSE_TYPE_JSON = "json";

        //*** Static configuration ***//
        internal static Type C8oHttpInterfaceUsed;
        internal static Type C8oFullSyncUsed;
        internal static Action<Action> defaultUiDispatcher;
        internal static string deviceUUID;

        //*** Attributes ***//

        /// <summary>
        /// The Convertigo endpoint, syntax : <protocol>://<server>:<port>/<Convertigo web app path>/projects/<project name> (Example : http://127.0.0.1:18080/convertigo/projects/MyProject)
        /// </summary>
        private string endpoint;
        private string endpointConvertigo;
        private bool endpointIsSecure;
        private string endpointHost;
        private string endpointPort;


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

        //*** Constructors ***//

        /// <summary>
        /// This is the base object representing a Convertigo Server end point. This object should be instanciated
        /// when the apps starts and be accessible from any class of the app. Although this is not common , you may have
        /// several C8o objects instanciated in your app.    
        /// </summary>
        /// <param name="endpoint">The End point url to you convertigo server. Can be :
        ///     - http(s)://your_server_address/convertigo/projects/your_project_name (if using an on premises server)
        ///     - http(s)://your_cloud_server.convertigo.net/cems/projects/your_project_name (if using a Convertigo cloud server)
        /// </param>
        /// <param name="c8oSettings">
        /// A C8oSettings object describing the endpoint configuration parameters such as authorizations credentials,
        /// cookies, client certificates and various other settings.
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
                httpInterface = C8oHttpInterfaceUsed.GetTypeInfo().DeclaredConstructors.ElementAt(1).Invoke(new object[] { this }) as C8oHttpInterface;
            }
            catch
            {
                httpInterface = new C8oHttpInterface(this);
            }

            c8oLogger = new C8oLogger(this);
            c8oLogger.SetRemoteLogParameters(httpInterface, LogRemote, endpointConvertigo, DeviceUUID);

            c8oLogger.LogMethodCall("C8o", this);

            try
            {
                c8oFullSync = C8oFullSyncUsed.GetTypeInfo().DeclaredConstructors.ElementAt(0).Invoke(new object[0]) as C8oFullSync;
            }
            catch
            {
                c8oFullSync = new C8oFullSyncHttp(FullSyncServerUrl, FullSyncUsername, FullSyncPassword);
            }

            try
            {
                c8oFullSync.Init(this);
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }
        }

        //*** C8o calls ***//
        public void Call(string requestable, IDictionary<string, object> parameters = null, C8oResponseListener c8oResponseListener = null, C8oExceptionListener c8oExceptionListener = null)
        {
            try
            {
                if (requestable == null)
                {
                    throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("Call requestable"));
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
                C8o.HandleCallException(c8oExceptionListener, parameters, e);
            }
        }

        public void Call(IDictionary<string, object> parameters = null, C8oResponseListener c8oResponseListener = null, C8oExceptionListener c8oExceptionListener = null)
        {
            // IMPORTANT : all c8o calls have to end here !
            try
            {
                c8oLogger.LogMethodCall("Call", parameters, c8oResponseListener, c8oExceptionListener);

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
                C8o.HandleCallException(c8oExceptionListener, parameters, e);
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

        /// <summary>
        /// Logs a message to Convertigo Server. the message will be seen in Convertigo Server Device logger. Logging messages to the server
        /// helps in monitoring Mobile apps in production.
        /// </summary>
        /// <param name="c8oLogLevel">Log level such as C8oLogLevel.DEBUG</param>
        /// <param name="message">The messe to be logged</param>
        /// <sample>
        ///     <code>myC8o.Log (C8oLogLevel.DEBUG, "This is my message");</code>
        /// </sample>
        public void Log(C8oLogLevel c8oLogLevel, string message)
        {
            c8oLogger.Log(c8oLogLevel, message);
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

        public override string ToString()
        {
            return "C8o[" + endpoint + "] " + base.ToString();
        }

        public string Endpoint
        {
            get { return endpoint; }
        }

        public string EndpointConvertigo
        {
            get { return endpointConvertigo; }
        }

        public bool EndpointIsSecure
        {
            get { return endpointIsSecure; }
        }

        public string EndpointHost
        {
            get { return endpointHost; }
        }

        public string EndpointPort
        {
            get { return endpointPort; }
        }

        public string DeviceUUID
        {
            get { return deviceUUID; }
        }

        public CookieContainer CookieStore
        {
            get { return httpInterface.CookieStore; }
        }

        private static IDictionary<string, object> ToParameters(object[] parameters)
        {
            if (parameters.Length % 2 != 0)
            {
                throw new System.ArgumentException("TODO");
            }

            var newParameters = new Dictionary<string, object>();

            for (var i = 0; i < parameters.Length; i += 2)
            {
                newParameters["" + parameters[i]] = parameters[i + 1];
            }

            return newParameters;
        }

        internal static void HandleCallException(C8oExceptionListener c8oExceptionListener, IDictionary<string, object> requestParameters, Exception exception)
        {
            C8oLogger.LogLocal(C8oLogLevel.ERROR, exception.Message);
            if (c8oExceptionListener != null)
            {
                c8oExceptionListener.OnException(exception, requestParameters);
            }
        }
    }
}
