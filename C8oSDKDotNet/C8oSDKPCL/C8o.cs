using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Text.RegularExpressions;
using Convertigo.SDK.Exceptions;
using System.Xml.Linq;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;
using Convertigo.SDK.Utils;
using Convertigo.SDK.FullSync;
using Convertigo.SDK.C8oEnum;
using System.Reflection;

// TODO
// doc
// log
// trust all certificates
// exception
// certificate client et serveur

namespace Convertigo.SDK
{

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
        public static Type C8oFullSyncUsed;

        //*** Attributes ***//

        /// <summary>
        /// The Convertigo endpoint, syntax : <protocol>://<server>:<port>/<Convertigo web app path>/projects/<project name> (Example : http://127.0.0.1:18080/convertigo/projects/MyProject)
        /// </summary>
        private string endpoint;
        private string endpointConvertigo;
        private bool endpointIsSecure;
        private string endpointHost;
        private string endpointPort;

        private string deviceUUID;

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

            httpInterface = new C8oHttpInterface(this);

            c8oLogger = new C8oLogger(this);
            c8oLogger.SetRemoteLogParameters(httpInterface, LogRemote, endpointConvertigo, deviceUUID);

            c8oLogger.LogMethodCall("C8o", this);

            try
            {
                c8oFullSync = C8oFullSyncUsed.GetTypeInfo().DeclaredConstructors.ElementAt(0).Invoke(new Object[0]) as C8oFullSync;
            }
            catch (Exception e)
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

        public C8oPromise<JObject> CallJson(string requestable, IDictionary<string, object> parameters)
        {
            var promise = new C8oPromise<JObject>(this);

            Call(requestable, parameters, new C8oResponseJsonListener((response, data) =>
            {
                if (data.ContainsKey(ENGINE_PARAMETER_PROGRESS))
                {
                    C8oProgress progress = data[ENGINE_PARAMETER_PROGRESS] as C8oProgress;
                    promise.OnProgress(progress);

                    if (progress.Stopped)
                    {
                        data.Remove(ENGINE_PARAMETER_PROGRESS);
                    }
                    else
                    {
                        return;
                    }
                }
                if (response != null)
                {
                    promise.OnResponse(response, data);
                }
            }), new C8oExceptionListener((exception, data) =>
            {
                promise.OnFailure(exception, data);
            }));

            return promise;
        }

        public C8oPromise<JObject> CallJson(string requestable, params object[] parameters)
        {
            return CallJson(requestable, ToParameters(parameters));
        }

        public C8oPromise<XDocument> CallXml(string requestable, IDictionary<string, object> parameters)
        {
            var promise = new C8oPromise<XDocument>(this);

            Call(requestable, parameters, new C8oResponseXmlListener((response, data) =>
            {
                if (data.ContainsKey(ENGINE_PARAMETER_PROGRESS))
                {
                    promise.OnProgress(data[ENGINE_PARAMETER_PROGRESS] as C8oProgress);
                }
                else
                {
                    promise.OnResponse(response, data);
                }
            }), new C8oExceptionListener((exception, data) =>
            {
                promise.OnFailure(exception, data);
            }));

            return promise;
        }

        public C8oPromise<XDocument> CallXml(string requestable, params object[] parameters)
        {
            return CallXml(requestable, ToParameters(parameters));
        }

        public void AddCookie(string name, string value)
        {
            httpInterface.AddCookie(name, value);
        }
        
        public void Log(C8oLogLevel c8oLogLevel, string message)
        {
            c8oLogger.Log(c8oLogLevel, message);
        }

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
