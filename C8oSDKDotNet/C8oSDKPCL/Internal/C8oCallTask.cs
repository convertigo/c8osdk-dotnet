using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Convertigo.SDK.Internal
{
    internal class C8oCallTask
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

            c8o.c8oLogger.LogMethodCall("C8oCallTask", parameters);
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
                c8o.Log._Debug("Is FullSync request");
                // The result cannot be handled here because it can be different depending to the platform
                // But it can be useful bor debug
                try
                {
                    var fullSyncResult = await c8o.c8oFullSync.HandleFullSyncRequest(parameters, c8oResponseListener);
                    return fullSyncResult;
                }
                catch (C8oException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw new C8oException(C8oExceptionMessage.FullSyncRequestFail(), e);
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
                C8oLocalCache localCache = C8oUtils.GetParameterObjectValue(parameters, C8oLocalCache.PARAM, false) as C8oLocalCache;
                bool localCacheEnabled = false;

                // If the engine parameter for local cache is specified
                if (localCache != null)
                {
                    // Removes local cache parameters and build the c8o call request identifier
                    parameters.Remove(C8oLocalCache.PARAM);

                    if (localCacheEnabled = localCache.enabled)
                    {
                        c8oCallRequestIdentifier = C8oUtils.IdentifyC8oCallRequest(parameters, responseType);

                        if (localCache.priority.IsAvailable(c8o))
                        {
                            try
                            {
                                C8oLocalCacheResponse localCacheResponse = await c8o.c8oFullSync.GetResponseFromLocalCache(c8oCallRequestIdentifier);
                                if (!localCacheResponse.Expired)
                                {
                                    if (responseType == C8o.RESPONSE_TYPE_XML)
                                    {
                                        return C8oTranslator.StringToXml(localCacheResponse.Response);
                                    }
                                    else if (responseType == C8o.RESPONSE_TYPE_JSON)
                                    {
                                        return C8oTranslator.StringToJson(localCacheResponse.Response);
                                    }
                                }
                            }
                            catch (C8oUnavailableLocalCacheException)
                            {
                                // no entry
                            }
                        }
                    }
                }

                //*** Get response ***//

                parameters[C8o.ENGINE_PARAMETER_DEVICE_UUID] = c8o.DeviceUUID;


                // Build the c8o call URL
                c8oCallUrl = c8o.Endpoint + "/." + responseType;

                HttpWebResponse httpResponse;

                try
                {
                    httpResponse = await c8o.httpInterface.HandleC8oCallRequest(c8oCallUrl, parameters);
                }
                catch (Exception e)
                {
                    if (localCacheEnabled)
                    {
                        try
                        {
                            C8oLocalCacheResponse localCacheResponse = await c8o.c8oFullSync.GetResponseFromLocalCache(c8oCallRequestIdentifier);
                            if (!localCacheResponse.Expired)
                            {
                                if (responseType == C8o.RESPONSE_TYPE_XML)
                                {
                                    return C8oTranslator.StringToXml(localCacheResponse.Response);
                                }
                                else if (responseType == C8o.RESPONSE_TYPE_JSON)
                                {
                                    return C8oTranslator.StringToJson(localCacheResponse.Response);
                                }
                            }
                        }
                        catch (C8oUnavailableLocalCacheException)
                        {
                            // no entry
                        }
                    }
                    return new C8oException(C8oExceptionMessage.handleC8oCallRequest(), e);
                }

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
                    long expirationDate = -1;
                    if (localCache.ttl > 0) {
                        expirationDate = localCache.ttl + C8oUtils.GetUnixEpochTime(DateTime.Now);
                    }
                    var localCacheResponse = new C8oLocalCacheResponse(responseString, responseType, expirationDate);
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
                else if (result is Exception)
                {
                    // The result is an Exception
                    c8o.HandleCallException(c8oExceptionListener, parameters, (Exception) result);
                }
                else
                {
                    // The result type is unknown
                    c8o.HandleCallException(c8oExceptionListener, parameters, new C8oException(C8oExceptionMessage.WrongResult(result)));
                }
                /*else if (result instanceof com.couchbase.lite.Document) {
                    // TODO log

                    // The result is a fillSync query response
                    ((C8oFullSyncResponseListener) this.c8oResponseListener).onDocumentResponse(this.parameters, (com.couchbase.lite.Document) result);
                } else if (result instanceof QueryEnumerator) {
                    // TODO log

                    // The result is a fillSync query response
                    ((C8oFullSyncResponseListener) this.c8oResponseListener).onQueryEnumeratorResponse(this.parameters, (QueryEnumerator) result);
                } */
            }
            catch (Exception e)
            {
                c8o.HandleCallException(c8oExceptionListener, parameters, e);
            }
        }
    }
}
