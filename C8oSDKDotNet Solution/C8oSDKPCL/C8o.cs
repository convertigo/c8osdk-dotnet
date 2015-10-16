using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK.Listeners;
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
using Convertigo.SDK.Http;
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

    public class C8o
    {
        //*** Regular Expression ***//

        /// <summary>
        /// The regex used to handle the c8o requestable syntax ("<project>.<sequence>" or "<project>.<connector>.<transaction>")
        /// </summary>
        private readonly Regex re_requestable = new Regex(@"^([^.]*)\.(?:([^.]+)|([^.]+)\.([^.]+))$", RegexOptions.IgnoreCase);
        /// <summary>
        /// The regex used to get the part of the endpoint before '/projects/'
        /// </summary>
        private readonly Regex re_endpoint = new Regex(@"^(http(s)?://([^:]+)(?::[0-9]+)?/[^/]+)/projects/[^/]+$", RegexOptions.IgnoreCase);
        /// <summary>
        /// The regex used to handle the c8o public key syntax
        /// </summary>
        private readonly Regex re_publicKey = new Regex(@"(.*?)\|(.*?)\|(.*?)", RegexOptions.IgnoreCase);

        //*** Engine reserved parameters ***//

        public static String ENGINE_PARAMETER_PROJECT = "__project";
        public static String ENGINE_PARAMETER_SEQUENCE = "__sequence";
        public static String ENGINE_PARAMETER_CONNECTOR = "__connector";
        public static String ENGINE_PARAMETER_TRANSACTION = "__transaction";
        public static String ENGINE_PARAMETER_ENCODED = "__encoded";
        public static String ENGINE_PARAMETER_LOCAL_CACHE = "__localCache";
        public static String ENGINE_PARAMETER_DEVICE_UUID = "__uuid";

        //*** Local cache keys ***//
	
	    public static String LOCAL_CACHE_PARAMETER_KEY_ENABLED = "enabled";
	    public static String LOCAL_CACHE_PARAMETER_KEY_POLICY = "policy";
	    public static String LOCAL_CACHE_PARAMETER_KEY_TTL = "ttl";
        public static String LOCAL_CACHE_DOCUMENT_KEY_RESPONSE = "response";
        public static String LOCAL_CACHE_DOCUMENT_KEY_RESPONSE_TYPE = "responseType";
        public static String LOCAL_CACHE_DOCUMENT_KEY_EXPIRATION_DATE = "expirationDate";
	
	    public static String LOCAL_CACHE_DATABASE_NAME = "c8olocalcache";

        //*** Response type ***//

        // public static String RESPONSE_TYPE_XML = "pxml";
        // public static String RESPONSE_TYPE_JSON = "json";

        //*** Constants ***//

        private static String[] fullSyncMobileAssemblies = { "C8oFullSyncNetAndroid", "C8oFullSyncNetiOS" };
        private static String fullSyncMobileClassName = "Convertigo.SDK.FullSync.FullSyncMobile";

        //*** Static configuration ***//
        public static Type FullSyncInterfaceUsed;

        //*** Attributes ***//

        /// <summary>
        /// The Convertigo endpoint, syntax : <protocol>://<server>:<port>/<Convertigo web app path>/projects/<project name> (Example : http://127.0.0.1:18080/convertigo/projects/MyProject)
        /// </summary>
        private String endpoint;
        /// <summary>
        /// Contains the results of the regex applied to the endpoint.
        /// </summary>
        private String[] endpointGroups;
        /// <summary>
        /// Contains C8o settings.
        /// </summary>
        public C8oSettings c8oSettings;
        /// <summary>
        /// Define the behavior when there is an exception during execution except during c8o calls with a defined C8oExceptionListener
        /// </summary>
        private C8oExceptionListener c8oExceptionListener;
        /// <summary>
        /// Contains cookies used to make HTTP calls. 
        /// </summary>
        private CookieContainer cookieContainer;
        /// <summary>
        /// Allows to log locally and remotely to the Convertigo server.
        /// </summary>
        private C8oLogger c8oLogger;
        /// <summary>
        /// Allows to make fullSync calls.
        /// </summary>
        private FullSyncInterface fullSyncInterface;
        private HttpInterface httpInterface;

        //*** Constructors ***//

        /// <summary>
        /// Initializes a new instance of the C8o class.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="c8oSettings"></param>
        /// <param name="c8oExceptionListener"></param>
        public C8o(String endpoint, C8oSettings c8oSettings = null, C8oExceptionListener c8oExceptionListener = null)
        {
            //*** Checks parameters validity ***//

            // Checks if the endpoint is a valid url
            if (!C8oUtils.IsValidUrl(endpoint))
            {
                throw new System.ArgumentException(C8oExceptionMessage.InvalidArgumentInvalidURL(endpoint));
            }
            // Checks the endpoint validty
            Match match = this.re_endpoint.Match(endpoint);
            if (!match.Success)
            {
                throw new System.ArgumentException(C8oExceptionMessage.InvalidArgumentInvalidEndpoint(endpoint));
            }

            if (c8oSettings == null)
            {
                c8oSettings = new C8oSettings();
            }

            //*** Initializes attributes ***//
            this.c8oSettings = c8oSettings;
            this.endpoint = endpoint;
            this.c8oExceptionListener = c8oExceptionListener;
            this.endpointGroups = new String[4];
            for (int i = 0; i < this.endpointGroups.Length; i++)
            {
                this.endpointGroups[i] = match.Groups[i].Value;
            }

            bool trustAllCertificates = c8oSettings.trustAllCetificates;
            this.cookieContainer = new CookieContainer();
            if (c8oSettings.cookies != null)
            {
                this.cookieContainer.Add(new Uri(endpoint), c8oSettings.cookies);
            }
            this.httpInterface = new HttpInterface(c8oSettings);

            /** Android OK but iOS KO
            // Retrieves the fullSyncMobile class thanks to reflection
            Boolean fullSyncMobileAssemblyFound = false;
            Assembly fullSyncMobileAssembly = null;

            int fullSyncMobileAssemblyC = 0;
            while (!fullSyncMobileAssemblyFound && fullSyncMobileAssemblyC < C8o.fullSyncMobileAssemblies.Length)
            {
                try
                {
                    AssemblyName assemblyName = new AssemblyName(fullSyncMobileAssemblies[fullSyncMobileAssemblyC++]);
                    fullSyncMobileAssembly = Assembly.Load(assemblyName);
                    fullSyncMobileAssemblyFound = true;
                }
                catch (Exception e)
                {
                    // Do nothing
                }
            }

            if (fullSyncMobileAssemblyFound)
            {
                try
                {
                    Type fullSyncInterfaceType = fullSyncMobileAssembly.GetType(fullSyncMobileClassName);
                    TypeInfo fullSyncMobileTypeInfo = fullSyncInterfaceType.GetTypeInfo();
                    if (!typeof(FullSyncInterface).GetTypeInfo().IsAssignableFrom(fullSyncMobileTypeInfo))
                    {
                        throw new C8oException(C8oExceptionMessage.ToDo());
                    }

                    Boolean fullSyncMobileConstructorFound = false;
                    IEnumerator<ConstructorInfo> constructors = fullSyncMobileTypeInfo.DeclaredConstructors.GetEnumerator();
                    ConstructorInfo constructor = null;
                    while (!fullSyncMobileConstructorFound && constructors.MoveNext())
                    {
                        constructor = constructors.Current;
                        if (constructor.GetParameters().Length == 0)
                        {
                            fullSyncMobileConstructorFound = true;
                        }
                    }
                    if (!fullSyncMobileConstructorFound)
                    {
                        throw new C8oException(C8oExceptionMessage.ToDo());
                    }
                    Object[] constructorParameters = {};
                    Object fullSyncMobile = constructor.Invoke(constructorParameters);
                    this.fullSyncInterface = (FullSyncInterface)fullSyncMobile;
                }
                catch (Exception e)
                {
                    throw new C8oException(C8oExceptionMessage.ToDo(), e);
                }
            }
            else
            {
                this.fullSyncInterface = new FullSyncHttp(c8oSettings.fullSyncServerUrl, c8oSettings.fullSyncUsername, c8oSettings.fullSyncPassword);
            }
*/

            this.c8oLogger = new C8oLogger(this.c8oExceptionListener, c8oSettings);
            this.c8oLogger.SetRemoteLogParameters(this.httpInterface, true, this.endpointGroups[1], "deviceUuid");

            // Log the method call
            this.c8oLogger.LogMethodCall("C8o", c8oSettings, c8oExceptionListener);

            // If https request have to trust all certificates
            if (trustAllCertificates)
            {
                // Unavailable :(
            }

            try
            {
                this.fullSyncInterface = FullSyncInterfaceUsed.GetTypeInfo().DeclaredConstructors.ElementAt(0).Invoke(new Object[0]) as FullSyncInterface;
            }
            catch (Exception e)
            {
                fullSyncInterface = new FullSyncHttp(c8oSettings.fullSyncServerUrl, c8oSettings.fullSyncUsername, c8oSettings.fullSyncPassword);
            }

            try
            {
                this.fullSyncInterface.Init(this, c8oSettings, this.endpointGroups[1]);
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }
        }

        //*** Private Utilities ***//

        internal static void HandleException(C8oExceptionListener c8oExceptionListener, Dictionary<String, Object> requestParameters, Exception exception)
        {
            C8oLogger.LogLocal(C8oLogLevel.ERROR, exception.Message);
            if (c8oExceptionListener != null)
            {
                c8oExceptionListener.OnException(exception, requestParameters);
            }
        }

        //*** C8o calls ***//        

        public void Call(String requestable, Dictionary<String, Object> parameters = null, C8oResponseListener c8oResponseListener = null)
        {
            this.Call(requestable, parameters, c8oResponseListener, this.c8oExceptionListener);
        }

        public void Call(String requestable, Dictionary<String, Object> parameters = null, C8oResponseListener c8oResponseListener = null, C8oExceptionListener c8oExceptionListener = null)
        {
            try
            {
                // Checks parameters validity
                if (parameters == null)
                {
                    // throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("Call parameters"));
                    parameters = new Dictionary<String, Object>();
                }
                if (requestable == null)
                {
                    throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("Call requestable"));
                }

                // Use the requestable String to add parameters corresponding to the c8o project, sequence, connector and transaction (<project>.<sequence> or <project>.<connector>.<transaction>)
                Match match = re_requestable.Match(requestable);
                if (!match.Success)
                {
                    // The requestable is not correct so the default transaction of the default connector will be called
                    throw new System.ArgumentException(C8oExceptionMessage.InvalidRequestable(requestable));
                }

                // If the project name is specified
                if (match.Groups[1].Value != "")
                {
                    parameters.Add(ENGINE_PARAMETER_PROJECT, match.Groups[1].Value);
                }
                // If the C8o call use a sequence
                if (match.Groups[2].Value != "")
                {
                    parameters.Add(ENGINE_PARAMETER_SEQUENCE, match.Groups[2].Value);
                }
                else
                {
                    parameters.Add(ENGINE_PARAMETER_CONNECTOR, match.Groups[3].Value);
                    parameters.Add(ENGINE_PARAMETER_TRANSACTION, match.Groups[4].Value);
                }

                this.Call(parameters, c8oResponseListener, c8oExceptionListener);
            }
            catch (Exception e)
            {
                C8o.HandleException(c8oExceptionListener, parameters, e);
            }
        }

        public void Call(Dictionary<String, Object> parameters = null, C8oResponseListener c8oResponseListener = null)
        {
            this.Call(parameters, c8oResponseListener, this.c8oExceptionListener);
        }

        public void Call(Dictionary<String, Object> parameters = null, C8oResponseListener c8oResponseListener = null, C8oExceptionListener c8oExceptionListener = null)
        {
            // IMPORTANT : all c8o calls have to end here !

            try
            {
                // Logs the method call (Only log c8o call here !)
                this.c8oLogger.LogMethodCall("Call", parameters, c8oResponseListener, c8oExceptionListener);

                // Checks parameters validity
                if (parameters == null)
                {
                    // throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("Call parameters"));
                    parameters = new Dictionary<String, Object>();
                }
                /*if (c8oResponseListener == null)
                {
                    throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("C8oExceptionListener"));
                }*/

                // Creates a async task running on another thread
                // Exceptions have to be handled by the C8oExceptionListener
                Task task = new Task(async () =>
                {    
                    try
                    {
                    // Checks if this is a fullSync request
                    Boolean isFullSyncRequest = FullSyncUtils.IsFullSyncRequest(parameters);
                    if (isFullSyncRequest)
                    {
                        this.c8oLogger.Log(C8oLogLevel.DEBUG, "Is FullSync request");
                        // The result cannot be handled here because it can be different depending to the platform
                        // But it can be useful bor debug
                        try
                        {
                            Object fullSyncResult = this.fullSyncInterface.HandleFullSyncRequest(parameters, c8oResponseListener);
                        }
                        catch (Exception e)
                        {
                            throw new C8oException(C8oExceptionMessage.ToDo(), e);
                        }

                    }
                    // Or else if this is an HTTP request
                    else
                    {
                        C8oHttpResponseListener httpResponseListener;
                        if (c8oResponseListener is C8oHttpResponseListener)
                        {
                            httpResponseListener = (C8oHttpResponseListener)c8oResponseListener;
                        }
                        else
                        {
                            throw new C8oException(C8oExceptionMessage.ToDo());
                        }
                            this.c8oLogger.Log(C8oLogLevel.DEBUG, "Starts the asynchronous task");
                            this.c8oLogger.Log(C8oLogLevel.DEBUG, "Is HTTP request");
                            // Defines the response type
                            ResponseType responseType = httpResponseListener.ResponseType;

                            //*** Local cache ***//

                            String c8oCallRequestIdentifier = null;
                            Boolean localCacheEnabled = false;
                            long timeToLive = -1;

                            // Checks if local cache parameters are sent
                            IDictionary<String, Object> localCacheParameters;
                            if (C8oUtils.TryGetParameterObjectValue <IDictionary<String, Object>>(parameters, C8o.ENGINE_PARAMETER_LOCAL_CACHE, out localCacheParameters))
                            {
                                // Checks if the local cache is enabled, if it is not then the local cache is enabled by default
                                C8oUtils.TryGetParameterObjectValue<Boolean>(localCacheParameters, C8o.LOCAL_CACHE_PARAMETER_KEY_ENABLED, out localCacheEnabled, defaultValue: true);
                                if (localCacheEnabled)
                                {
                                    // Removes local cache parameters and build the c8o call request identifier
                                    parameters.Remove(C8o.ENGINE_PARAMETER_LOCAL_CACHE);
                                    c8oCallRequestIdentifier = C8oUtils.IdentifyC8oCallRequest(parameters, responseType);
                                    // Unused to retrieve the response but used to store the response
                                    C8oUtils.TryGetParameterObjectValue<long>(localCacheParameters, C8o.LOCAL_CACHE_PARAMETER_KEY_TTL, out timeToLive, defaultValue: timeToLive);

                                    // Retrieves the local cache policy
                                    String localCachePolicyStr;
                                    if (C8oUtils.TryGetParameterObjectValue<String>(localCacheParameters, C8o.LOCAL_CACHE_PARAMETER_KEY_POLICY, out localCachePolicyStr))
                                    {
                                        LocalCachePolicy localCachePolicy;
                                        if (LocalCachePolicy.TryGetLocalCachePolicy(localCachePolicyStr, out localCachePolicy))
                                        {
                                            if (localCachePolicy.IsAvailable())
                                            {
                                                try
                                                {
                                                    LocalCacheResponse localCacheResponse = this.fullSyncInterface.GetResponseFromLocalCache(c8oCallRequestIdentifier);
                                                    if (!localCacheResponse.Expired())
                                                    {
                                                        httpResponseListener.OnStringResponse(localCacheResponse.Response, parameters);
                                                        return;
                                                    }
                                                }
                                                catch (C8oUnavailableLocalCacheException e)
                                                {
                                                    // does nothing
                                                }
                                            }
                                        }
                                        else
                                        {
                                            throw new ArgumentException(C8oExceptionMessage.ToDo());
                                        }
                                    }
                                    else
                                    {
                                        throw new ArgumentException(C8oExceptionMessage.ToDo());
                                    }
                                }
                            }

                            //*** Get response ***//

                            // Builds the HTTP request and executes it
                            String url = endpoint + "/." + responseType.Value;
                            WebResponse webResponse;
                            try
                            {
                                // webResponse = await HttpInterface.HandleRequest(url, parameters, this.timeout, this.cookieContainer);
                                webResponse = await this.httpInterface.HandleRequest(url, parameters, this.cookieContainer);
                            }
                            catch (Exception e)
                            {
                                throw new C8oException(C8oExceptionMessage.ToDo(), e);
                            }
                            Stream responseStream = webResponse.GetResponseStream();

                            //*** Handles response depending to its type ***//

                            String responseString = httpResponseListener.OnStreamResponse(responseStream, parameters, localCacheEnabled);

                            if (localCacheEnabled)
                            {
                                // String responseString = C8oTranslator.StreamToString(responseStream);
                                long expirationdate = timeToLive;
                                if (expirationdate > 0) {
                                    expirationdate = expirationdate + C8oUtils.GetUnixEpochTime(DateTime.Now);
                                }
                                LocalCacheResponse localCacheResponse = new LocalCacheResponse(responseString, httpResponseListener.ResponseType, expirationdate);
                                this.fullSyncInterface.SaveResponseToLocalCache(c8oCallRequestIdentifier, localCacheResponse);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        C8o.HandleException(c8oExceptionListener, parameters, e);
                    }
                });
                task.Start();
            }
            catch (Exception e)
            {
                C8o.HandleException(c8oExceptionListener, parameters, e);
            }
        }

        public void Log(C8oLogLevel c8oLogLevel, String message)
        {
            this.c8oLogger.Log(c8oLogLevel, message);
        }

        //*** Getter / Setter ***//
        
        public CookieCollection GetCookies()
        {
            CookieCollection cookies = this.cookieContainer.GetCookies(new Uri(this.endpoint));
            return cookies;
        }

        public String GetEndpointPart(int i)
        {
            return this.endpointGroups[i];
        }

        public Task<JObject> CallJsonAsync(String requestable, Dictionary<String, Object> parameters = null)
        {
            TaskCompletionSource<JObject> task = new TaskCompletionSource<JObject>();

            Call(requestable, parameters, new C8oJsonResponseListener((jsonResponse, data) =>
            {
                task.TrySetResult(jsonResponse);
            }), new C8oExceptionListener((exception, data) =>
            {
                task.TrySetException(exception);
            }));

            return task.Task;
        }

        public Task<XDocument> CallXmlAsync(String requestable, Dictionary<String, Object> parameters = null)
        {
            TaskCompletionSource<XDocument> task = new TaskCompletionSource<XDocument>();

            Call(requestable, parameters, new C8oXmlResponseListener((xmlResponse, data) =>
            {
                task.TrySetResult(xmlResponse);
            }), new C8oExceptionListener((exception, data) =>
            {
                task.TrySetException(exception);
            }));

            return task.Task;
        }

    }
}
