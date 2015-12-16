using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Convertigo.SDK.Internal
{
    class C8oCallTask
    {
        private C8o c8o;
        private IDictionary<string, object> parameters;
        private C8oResponseListener c8oResponseListener;
        private C8oExceptionListener c8oExceptionListener;
        private string c8oCallUrl;

        public C8oCallTask(C8o c8o, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener, C8oExceptionListener c8oExceptionListener)
        {
            this.c8o = c8o;
            this.parameters = parameters;
            this.c8oResponseListener = c8oResponseListener;
            this.c8oExceptionListener = c8oExceptionListener;

            c8o.c8oLogger.LogMethodCall("C8oCallTask", parameters, c8oResponseListener, c8oExceptionListener);
        }

        public void Execute()
        {
            Task.Run((Action) DoInBackground);
        }

        async private void DoInBackground()
        {
            try
            {
                var response = await HandleRequest();
                HandleResponse(response);
            }
            catch (Exception e)
            {
                c8oExceptionListener.OnException(e, null);
            }
        }


        async private Task<object> HandleRequest()
        {
            bool isFullSyncRequest = C8oFullSync.IsFullSyncRequest(parameters);

            if (isFullSyncRequest)
            {
                c8o.Log(C8oLogLevel.DEBUG, "Is FullSync request");
                // The result cannot be handled here because it can be different depending to the platform
                // But it can be useful bor debug
                try
                {
                    var fullSyncResult = await c8o.c8oFullSync.HandleFullSyncRequest(parameters, c8oResponseListener);
                    return fullSyncResult;
                }
                catch (Exception e)
                {
                    throw new C8oException(C8oExceptionMessage.ToDo(), e);
                }
            }
            else
            {
                string responseType;
                if (c8oResponseListener == null || c8oResponseListener is C8oResponseXmlListener)
                {
                    responseType = C8o.RESPONSE_TYPE_XML;
                }
                else if (c8oResponseListener is C8oResponseJsonListener)
                {
                    responseType = C8o.RESPONSE_TYPE_JSON;
                }
                else
                {
                    return new C8oException("wrong listener");
                }

                //*** Local cache ***//

                string c8oCallRequestIdentifier = null;

                // Allows to enable or disable the local cache on a Convertigo requestable, default value is true
                bool localCacheEnabled = false;

                // Defines the time to live of the cached response, in milliseconds
                long localCacheTimeToLive = -1;
                
                // Checks if the local cache must be used
                object localCacheParameterValue = C8oUtils.GetParameterObjectValue(parameters, C8o.ENGINE_PARAMETER_LOCAL_CACHE, false);

                // If the engine parameter for local cache is specified
                if (localCacheParameterValue != null)
                {
                    var localCacheParameters = localCacheParameterValue as IDictionary<string, object>;
                    // Checks if the local cache is enabled, if it is not then the local cache is enabled by default
                    C8oUtils.TryGetParameterObjectValue<Boolean>(localCacheParameters, C8o.LOCAL_CACHE_PARAMETER_KEY_ENABLED, out localCacheEnabled, defaultValue: true);
                    if (localCacheEnabled)
                    {
                        // Removes local cache parameters and build the c8o call request identifier
                        parameters.Remove(C8o.ENGINE_PARAMETER_LOCAL_CACHE);

                        c8oCallRequestIdentifier = C8oUtils.IdentifyC8oCallRequest(parameters, responseType);
                        // Unused to retrieve the response but used to store the response
                        C8oUtils.TryGetParameterObjectValue<long>(localCacheParameters, C8o.LOCAL_CACHE_PARAMETER_KEY_TTL, out localCacheTimeToLive, defaultValue: localCacheTimeToLive);

                        // Retrieves the local cache policy
                        string localCachePolicyStr;
                        if (C8oUtils.TryGetParameterObjectValue<String>(localCacheParameters, C8o.LOCAL_CACHE_PARAMETER_KEY_POLICY, out localCachePolicyStr))
                        {
                            LocalCachePolicy localCachePolicy;
                            if (LocalCachePolicy.TryGetLocalCachePolicy(localCachePolicyStr, out localCachePolicy))
                            {
                                if (localCachePolicy.IsAvailable())
                                {
                                    try
                                    {
                                        C8oLocalCacheResponse localCacheResponse = await c8o.c8oFullSync.GetResponseFromLocalCache(c8oCallRequestIdentifier);
                                        if (!localCacheResponse.Expired)
                                        {
                                            //httpResponseListener.OnStringResponse(localCacheResponse.Response, parameters);
                                            //return;
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

                parameters[C8o.ENGINE_PARAMETER_DEVICE_UUID] = c8o.DeviceUUID;


                // Build the c8o call URL
                c8oCallUrl = c8o.Endpoint + "/." + responseType;

                var httpResponse = await c8o.httpInterface.HandleC8oCallRequest(c8oCallUrl, parameters);
                var responseStream = httpResponse.GetResponseStream();

                object response;
                string responseString = null;
                if (c8oResponseListener is C8oResponseXmlListener)
                {
                    response = C8oTranslator.StreamToXml(responseStream);
                }
                else if (c8oResponseListener is C8oResponseJsonListener)
                {
                    responseString = C8oTranslator.StreamToString(responseStream);
                    response = C8oTranslator.StringToJson(responseString);
                }
                else
                {
                    return new C8oException("wrong listener");
                }

                if (localCacheEnabled)
                {
                    // String responseString = C8oTranslator.StreamToString(responseStream);
                    long expirationdate = localCacheTimeToLive;
                    if (expirationdate > 0) {
                        expirationdate = expirationdate + C8oUtils.GetUnixEpochTime(DateTime.Now);
                    }
                    var localCacheResponse = new C8oLocalCacheResponse(responseString, null, expirationdate);
                    await c8o.c8oFullSync.SaveResponseToLocalCache(c8oCallRequestIdentifier, localCacheResponse);
                }

                return response;
            }
        }
            private void HandleResponse(object result) {
                try
                {
                    if (result is VoidResponse)
                    {
                        return;
                    }

                    if (c8oResponseListener == null)
                    {
                        return;
                    }

                    if (result is XDocument)
                    {
                        c8o.c8oLogger.LogC8oCallXMLResponse(result as XDocument, c8oCallUrl, parameters);
                        (c8oResponseListener as C8oResponseXmlListener).OnXmlResponse(result as XDocument, parameters);
                    }
                    else if (result is JObject)
                    {
                        c8o.c8oLogger.LogC8oCallJSONResponse(result as JObject, c8oCallUrl, parameters);
                        (c8oResponseListener as C8oResponseJsonListener).OnJsonResponse(result as JObject, parameters);
                    }
                    /*else if (result instanceof com.couchbase.lite.Document) {
                        // TODO log

                        // The result is a fillSync query response
                        ((C8oFullSyncResponseListener) this.c8oResponseListener).onDocumentResponse(this.parameters, (com.couchbase.lite.Document) result);
                    } else if (result instanceof QueryEnumerator) {
                        // TODO log

                        // The result is a fillSync query response
                        ((C8oFullSyncResponseListener) this.c8oResponseListener).onQueryEnumeratorResponse(this.parameters, (QueryEnumerator) result);
                    } else if (result instanceof Exception){
                        // The result is an Exception
                        C8o.handleCallException(this.c8oExceptionListener, this.parameters, (Exception) result);
                    } else {
                        // The result type is unknown
                        C8o.handleCallException(this.c8oExceptionListener, this.parameters, new C8oException(C8oExceptionMessage.WrongResult(result)));
                    }*/
                }
                catch (Exception e)
                {
                    C8o.HandleCallException(c8oExceptionListener, parameters, e);
                }
            }
    }
}
