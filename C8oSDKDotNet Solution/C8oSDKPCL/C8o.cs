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

        public static String RESPONSE_TYPE_XML = "pxml";
        public static String RESPONSE_TYPE_JSON = "json";

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
            if (c8oSettings.fullSyncInterface != null)
            {
                this.fullSyncInterface = c8oSettings.fullSyncInterface;
                try
                {
                    this.fullSyncInterface.Init(this, c8oSettings, this.endpointGroups[1]);
                }
                catch (Exception e)
                {
                    throw new C8oException(C8oExceptionMessage.ToDo(), e);
                }
            }

            this.c8oLogger = new C8oLogger(this.c8oExceptionListener, c8oSettings);
            this.c8oLogger.SetRemoteLogParameters(null, true, this.endpointGroups[1], "deviceUuid");

            // Log the method call
            this.c8oLogger.LogMethodCall("C8o", c8oSettings, c8oExceptionListener);

            // If https request have to trust all certificates
            if (trustAllCertificates)
            {
                // Unavailable :(
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

        public void Call(String requestable, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            this.Call(requestable, parameters, c8oResponseListener, this.c8oExceptionListener);
        }

        public void Call(String requestable, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener, C8oExceptionListener c8oExceptionListener)
        {
            try
            {
                // Checks parameters validity
                if (parameters == null)
                {
                    throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("Call parameters"));
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

        public void Call(Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            this.Call(parameters, c8oResponseListener, this.c8oExceptionListener);
        }

        public void Call(Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener, C8oExceptionListener c8oExceptionListener)
        {
            // IMPORTANT : all c8o calls have to end here !

            try
            {
                // Logs the method call (Only log c8o call here !)
                this.c8oLogger.LogMethodCall("Call", parameters, c8oResponseListener, c8oExceptionListener);

                // Checks parameters validity
                if (parameters == null)
                {
                    throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("Call parameters"));
                }
                if (c8oResponseListener == null)
                {
                    throw new System.ArgumentNullException(C8oExceptionMessage.InvalidArgumentNullParameter("C8oExceptionListener"));
                }

                
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
                        // Creates a async task running on another thread
                        // Exceptions have to be handled by the C8oExceptionListener
                        Task task = new Task(async () =>
                        {
                            try
                            {
                            this.c8oLogger.Log(C8oLogLevel.DEBUG, "Starts the asynchronous task");
                            this.c8oLogger.Log(C8oLogLevel.DEBUG, "Is HTTP request");
                            // Defines the response type
                            String responseType = "";
                            if (c8oResponseListener is C8oXmlResponseListener)
                            {
                                responseType = RESPONSE_TYPE_XML;
                            }
                            else if (c8oResponseListener is C8oJsonResponseListener)
                            {
                                responseType = RESPONSE_TYPE_JSON;
                            }
                            else
                            {
                                throw new System.ArgumentException(C8oExceptionMessage.WrongListener(c8oResponseListener));
                            }

                            //*** Local cache ***//

                            //String c8oCallRequestIdentifier = null;
                            //// Allows to enable or disable the local cache on a Convertigo requestable
                            //Boolean localCacheEnabledParameterValue = false;
                            //// Defines the time to live of the cached response, in milliseconds
                            //int localCacheTimeToLiveParameterValue = 0;
                            // Checks if the local cache have to be used
                            //Object localCacheParameterValue;
                            //try {
                            //    localCacheParameterValue = C8oUtils.GetParameterObjectValue(parameters, C8o.ENGINE_PARAMETER_LOCAL_CACHE, false, typeof(IDictionary<String, Object>));
                            //} catch (C8oException e) {
                            //    throw new C8oException(C8oExceptionMessage.getNameValuePairObjectValue(C8o.ENGINE_PARAMETER_LOCAL_CACHE), e);
                            //}
                            //// If the engine parameter for local cache is specified
                            //if (localCacheParameterValue != null) {
                            //    // Checks if this is a JSON object (represented by a IDictionary once translated) 
                            //    if (localCacheParameterValue is IDictionary<String, Object>) {
                            //        IDictionary<String, Object> localCacheParameterValueJson = (IDictionary<String, Object>) localCacheParameterValue;
                            //        // The local cache policy
                            //        String localCachePolicyParameterValue;
                            //        try {
                            //            // Gets local cache properties
                            //            localCacheEnabledParameterValue = C8oUtils.getAndCheck(localCacheParameterValueJson, C8o.LOCAL_CACHE_PARAMETER_KEY_ENABLED, Boolean.class, false, Boolean.TRUE);
                            //            localCachePolicyParameterValue = C8oUtils.getAndCheck(localCacheParameterValueJson, C8o.LOCAL_CACHE_PARAMETER_KEY_POLICY, String.class, true, null);
                            //            localCacheTimeToLiveParameterValue = C8oUtils.getAndCheck(localCacheParameterValueJson, C8o.LOCAL_CACHE_PARAMETER_KEY_TTL, Integer.class, false, localCacheTimeToLiveParameterValue);
                            //        } catch (C8oException e) {
                            //            return new C8oException(C8oExceptionMessage.getLocalCacheParameters(), e);
                            //        }
                            //        if (localCacheEnabledParameterValue) {
                            //            // Checks if the same request is stored in the local cache
                            //            C8oUtils.removeParameter(parameters, C8o.ENGINE_PARAMETER_LOCAL_CACHE);
                            //            try {
                            //                c8oCallRequestIdentifier = C8oUtils.identifyC8oCallRequest(parameters, responseType);
                            //            } catch (C8oException e) {
                            //                return new C8oException(C8oExceptionMessage.serializeC8oCallRequest(), e);
                            //            }
                            //            try {
                            //                localCacheDocument = C8o.this.getDocumentFromLocalCache(c8oCallRequestIdentifier);
                            //            } catch (C8oException e) {
                            //                return new C8oException(C8oExceptionMessage.getResponseFromLocalCache(), e);
                            //            }
                            //            try {
                            //                return C8o.this.getResponseFromLocalCacheDocument(localCacheDocument, localCachePolicyParameterValue);
                            //            } catch (C8oException e) {
                            //                return new C8oException(C8oExceptionMessage.getResponseFromLocalCacheDocument(), e);
                            //            } catch (C8oUnavailableLocalCacheException e) {
                            //                // Does nothing because in this case it means that the local cache is unavailable for this request
                            //            }
                            //        }
                            //    } else {
                            //        return new IllegalArgumentException(C8oExceptionMessage.toDo());
                            //    }
                            //}

                            //*** Get response ***//

                            // Builds the HTTP request and executes it
                            String url = endpoint + "/." + responseType;
                            WebResponse webResponse;
                            try
                            {
                                //Task task = new Task(async () =>
                                //{
                                //    webResponse = await HttpInterface.HandleRequest(url, parameters, this.cookieContainer);
                                //});
                                //task.Wait();
                                webResponse = await HttpInterface.HandleRequest(url, parameters, this.cookieContainer);
                            }
                            catch (Exception e)
                            {
                                throw new C8oException(C8oExceptionMessage.ToDo(), e);
                            }
                            Stream responseStream = webResponse.GetResponseStream();

                            //*** Handles response depending to its type ***//

                            // Converts the response stream depending to the response listener
                            if (c8oResponseListener is C8oXmlResponseListener)
                            {
                                // Converts the Stream to XDocument
                                XDocument responseDocument = C8oTranslator.StreamToXml(responseStream);
                                ((C8oXmlResponseListener)c8oResponseListener).OnXmlResponse(responseDocument, parameters);
                            }
                            else if (c8oResponseListener is C8oJsonResponseListener)
                            {
                                //// Converts the Stream to String then String to JObject
                                JObject responseJson = C8oTranslator.StreamToJson(responseStream);
                                ((C8oJsonResponseListener)c8oResponseListener).OnJsonResponse(responseJson, parameters);
                            }
                            else
                            {
                                // Should never happen because it was already checked before
                            }
                        }
                        catch (Exception e)
                        {
                            C8o.HandleException(c8oExceptionListener, parameters, e);
                        }
                    });
                    task.Start();
                }
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



    }
}
