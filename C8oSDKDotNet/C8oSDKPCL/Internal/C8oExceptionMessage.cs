using Convertigo.SDK.Internal;
using System;
using System.Collections.Generic;

namespace Convertigo.SDK
{
    internal class C8oExceptionMessage
    {

        public static string NotImplementedFullSyncInterface()
        {
            return "You are using the default FullSyncInterface which is not implemented";
        }

        public static string InvalidParameterValue(string parameterName, string details = null)
        {
            string errorMessage = "The parameter '" + parameterName + "' is invalid";
            if (details != null) 
            {
                errorMessage += ", " + details;
            }
            return errorMessage;
        }

        public static string MissingValue(string valueName)
        {
            return "The " + valueName + " is missing";
        }

        public static string UnknownValue(string valueName, string value)
        {
            return "The " + valueName + " value " + value + " is unknown";
        }

        public static string UnknownType(string variableName, Object variable)
        {
            return "The " + variableName + " type " + C8oUtils.GetObjectClassName(variable) + "is unknown";
        }

        public static string RessourceNotFound(string ressourceName)
        {
            return "The " + ressourceName + " was not found";
        }

        public static string ToDo()
        {
            return "TODO";
        }





        //*** TAG Illegal argument ***//

        public static string illegalArgumentInvalidFullSyncDatabaseUrl(string fullSyncDatabaseUrlStr)
        {
            return "The fullSync database url '" + fullSyncDatabaseUrlStr + "' is not a valid url";
        }

        internal static string FullSyncDatabaseInitFailed(string databaseName)
        {
            return "Failed to initialize the FullSync database '" + databaseName + "'";
        }

        public static string MissParameter(string parameterName)
        {
            return "The parameter '" + parameterName + "' is missing";
        }

        private static string illegalArgumentInvalidParameterValue(string parameterName, string parameterValue)
        {
            return "'" + parameterValue + "' is not a valid value for the parameter '" + parameterName + "'";
        }

        //public static string illegalArgumentInvalidParameterProjectRequestableFullSync(string projectParameter)
        //{
        //    return C8oExceptionMessage.illegalArgumentInvalidParameterValue(C8o.ENGINE_PARAMETER_PROJECT, projectParameter) +
        //    ", to run a fullSync request this parameter must start with '" + FullSyncInterface.FULL_SYNC_PROJECT + "'";
        //}

        public static string InvalidArgumentInvalidURL(string urlStr)
        {
            return "'" + urlStr + "' is not a valid URL";
        }

        internal static string UnknownFullSyncPolicy(FullSyncPolicy policy)
        {
            return "Unknown the FullSync policy '" + policy + "'";
        }

        public static string InvalidArgumentInvalidEndpoint(string endpoint)
        {
            return "'" + endpoint + "' is not a valid Convertigo endpoint";
        }

        public static string InvalidRequestable(string requestable)
        {
            return "'" + requestable + "' is not a valid requestable.";
        }

        public static string InvalidParameterType(string parameterName, string wantedParameterType, string actualParameterType)
        {
            return "The parameter '" + parameterName + "' must be of type '" + wantedParameterType + "' and not '" + actualParameterType + "'";
        }

        public static string illegalArgumentIncompatibleListener(string listenerType, string responseType)
        {
            return "The listener type '" + listenerType + "' is incompatible with the response type '" + responseType + "'";
        }

        public static string InvalidArgumentNullParameter(string parameterName)
        {
            return parameterName + " must be not null";
        }

        //*** TAG Initialization ***//

        // TODO
        public static string InitError()
        {
            return "Unable to initialize ";
        }

        public static string InitRsaPublicKey()
        {
            return "Unable to initialize the RSA public key";
        }

        public static string InitCouchManager()
        {
            return "Unable to initialize the fullSync databases manager";
        }

        public static string InitSslSocketFactory()
        {
            return "Unable to initialize the ssl socket factory";
        }

        public static string InitDocumentBuilder()
        {
            return "Unable to initialize the XML document builder";
        }

        //*** TAG Parse ***//

        public static string ParseStreamToJson()
        {
            return "Unable to parse the input stream to a json document";
        }

        public static string ParseStreamToXml()
        {
            return "Unable to parse the input stream to an xml document";
        }

        public static string parseInputStreamToString()
        {
            return "Unable to parse the input stream to a string";
        }

        public static string parseXmlToString()
        {
            return "Unable to parse the xml document to a string";
        }

        public static string parseRsaPublicKey()
        {
            return "Unable to parse the RSA public key";
        }

        public static string parseQueryEnumeratorToJson()
        {
            return "Unable to parse the query to a json document";
        }

        public static string parseLogsToJson()
        {
            return "Unable to parse logs to a json document";
        }

        public static string parseLogsStreamToJson()
        {
            return "Unable to parse stream containing logs response to a json document";
        }

        public static string parseC8oReplicationResultToJson()
        {
            return "Unable to parse the replication result to a json document";
        }

        public static string parseFullSyncDefaultResponseToJson()
        {
            return "Unable to parse the default fullSync result to a json document";
        }

        public static string parseFullSyncPostDocumentResponseToJson()
        {
            return "Unable to parse the post document fullSync result to a json document";
        }

        public static string parseStringToJson()
        {
            return "Unable to parse the string to a JSON document";
        }

        public static string ParseStringToObject(Type type)
        {
            return "Unable to parse the string (JSON) to an object of type " + type;
        }

        public static string StringToJsonValue(string str)
        {
            return "Unable to translate the string '" + str + "' to a JSON value"; 
        }

        public static string GetParameterJsonValue(KeyValuePair<string, object> parameter)
        {
            return "Unable to translate the string value '" + parameter.Value + "' of the key + '" + parameter.Key + "' to a JSON value";
        }

        //*** TAG HTTP ***//

        public static string retrieveRsaPublicKey()
        {
            return "Error during http request to get the RSA public key";
        }

        public static string httpLogs()
        {
            return "Error during http request to send logs to the Convertigo server";
        }

        //*** TAG Couch ***//

        public static string couchRequestGetView()
        {
            return "Unable to run the view query";
        }

        public static string couchRequestAllDocuments()
        {
            return "Unable to run the all query";
        }

        public static string couchRequestResetDatabase()
        {
            return "Unable to run the reset query";
        }

        public static string couchRequestDeleteDocument()
        {
            return "Unable to run the delete document query";
        }

        public static string couchRequestInvalidRevision()
        {
            return "The revision is invalid";
        }

        public static string couchRequestPostDocument()
        {
            return "Unable to run the post document query";
        }

        public static string unableToGetFullSyncDatabase(string databaseName)
        {
            return "Unable to get the fullSync database '" + databaseName + "' from the manager";
        }

        public static string couchNullResult()
        {
            return "An error occured during the fullSync request, its result is null";
        }

        public static string couchFullSyncNotActive()
        {
            return "Unable to use fullSync because it was not activated at the initialization";
        }

        public static string CouchDeleteFailed()
        {
            return "Delete the Couch document failed";
        }

        //public static string fullSyncPutProperties(Map<string, object> properties)
        //{
        //    return "Unable to put the following properties in the fullSync Document : " + properties;
        //}

        public static string fullSyncGetOrCreateDatabase(string databaseName)
        {
            return "Unable to get or create the fullSync database '" + databaseName + "'";
        }

        //public static string fullSyncHandleRequest(string requestable, string databaseName, List<NameValuePair> parameters)
        //{
        //    return "Error while running the fullSync request, requestalbe='" + requestable + "', databaseName='" + databaseName + "', parameters=" + parameters;
        //}

        public static string fullSyncHandleResponse()
        {
            return "Error while handling the fullSync response";
        }

        //*** TAG Certificate ***//

        public static string loadKeyStore()
        {
            return "Failed to load key store";
        }

        public static string trustAllCertificates()
        {
            return "Unable to load a key store trusting all certificates";
        }

        public static string clientKeyStore()
        {
            return "Unable to load the client key store";
        }

        public static string serverKeyStore()
        {
            return "Unable to load the server key store";
        }

        //*** TAG Not found ***//

        public static string illegalArgumentNotFoundFullSyncView(string viewName, string databaseName)
        {
            return "Cannot found the view '" + viewName + "' in database '" + databaseName + "'";
        }

        //*** TAG Other ***//

        public static string unhandledResponseType(string responseType)
        {
            return "The response type '" + responseType + "' is not handled";
        }

        public static string unhandledListenerType(string listenerType)
        {
            return "The listener type '" + listenerType + "' is not handled";
        }

        public static string WrongListener(C8oResponseListener c8oListener)
        {
            return "The C8oListener class " + C8oUtils.GetObjectClassName(c8oListener) + " is not handled";
        }

        public static string WrongResult(Object result)
        {
            return "The response class " + C8oUtils.GetObjectClassName(result) + " is not handled";
        }

        public static string toDo()
        {
            return "todo";
        }

        public static string unhandledFullSyncRequestable(string fullSyncRequestableValue)
        {
            return "The fullSync requestable '" + fullSyncRequestableValue + "' is not handled";
        }

        public static string closeInputStream()
        {
            return "Unable to close the input stream";
        }

        public static string deserializeJsonObjectFromString(string str)
        {
            return "Unable to deserialize the JSON object from the following string : '" + str + "'";
        }

        //public static string getNameValuePairObjectValue(NameValuePair nameValuePair)
        //{
        //    return "Unable to get the value from the NameValuePair with name '" + nameValuePair.getName() + "'";
        //}

        public static string postDocument()
        {
            return "Unable to post document";
        }

        public static string getNameValuePairObjectValue(string name)
        {
            return "Unable to get the object value from the NameValuePair named '" + name + "'";
        }

        public static string queryEnumeratorToJSON()
        {
            return "Unable to parse the QueryEnumerato to a JSON document";
        }

        public static string queryEnumeratorToXML()
        {
            return "Unable to parse the QueryEnumerato to a XML document";
        }

        public static string addparametersToQuery()
        {
            return "Unable to add parameters to the fullSync query";
        }

        public static string putJson()
        {
            return "Failed to put data in JSON ...";
        }

        public static string changeEventToJson()
        {
            return "Failed to parse ChangeEvent to JSON document";
        }

        public static string initC8oSslSocketFactory()
        {
            return "Failed to initialize C8oSslSocketFactory";
        }

        public static string createSslContext()
        {
            return "failed to create a new SSL context";
        }

        public static string keyManagerFactoryInstance()
        {
            return "Failed to instanciate KeyManagerFactory";
        }

        public static string initKeyManagerFactory()
        {
            return "Failed to initialize the key manager factory";
        }

        public static string InitHttpInterface()
        {
            return "Failed to initialize the secure HTTP Interface";
        }

        public static string trustManagerFactoryInstance()
        {
            return "Failed to instanciate KeyManagerFactory";
        }

        public static string initTrustManagerFactory()
        {
            return "Failed to initialize the key manager factory";
        }

        public static string initSslContext()
        {
            return "Failed to initialize the SSL context";
        }

        public static string initCipher()
        {
            return "Failed to initialize the cipher";
        }

        public static string urlEncode()
        {
            return "Failed to URL encode prameters";
        }

        public static string getParametersStringBytes()
        {
            return "Failed to get parameters string bytes";
        }

        public static string encodeParameters()
        {
            return "Failed to encode parameters";
        }

        public static string RunHttpRequest()
        {
            return "Failed to run the HTTP request";
        }

        public static string generateRsaPublicKey()
        {
            return "Failed to generate RSA public key";
        }

        public static string keyFactoryInstance()
        {
            return "Failed to get KeyFactory instance";
        }

        public static string getCipherInstance()
        {
            return "Failed to get Cipher instance";
        }

        public static string entryNotFound(string entryKey)
        {
            return "Entry key '" + entryKey + "' not found";
        }

        public static string c8oCallRequestToJson()
        {
            return "Failed to parse c8o call request to JSON";
        }

        public static string getJsonKey(string key)
        {
            return "Failed to get the JSON key '" + key + "'";
        }

        public static string jsonValueToXML()
        {
            return "Failed to parse JSON value to XML";
        }

        public static string inputStreamToXML()
        {
            return "Failed to parse InputStream to an XML document";
        }

        public static string inputStreamReaderEncoding()
        {
            return "Failed to instanciate the InputStreamReader";
        }

        public static string readLineFromBufferReader()
        {
            return "Failed to read line from the BufferReader";
        }

        public static string GetLocalCacheParameters()
        {
            return "Failed to get local cache parameters";
        }

        public static string GetLocalCachePolicy(string policy)
        {
            return "Failed to get local cache policy: " + policy;
        }

        public static string fullSyncJsonToXML()
        {
            return "Failed to translate full sync JSON to XML";
        }

        public static string takeLog()
        {
            return "Failed to take a log line in the list";
        }

        public static string remoteLogHttpRequest()
        {
            return "Failed while running the HTTP request sending logs to the Convertigo server";
        }

        public static string getInputStreamFromHttpResponse()
        {
            return "Failed to get InputStream from the HTTP response";
        }

        public static string inputStreamToJSON()
        {
            return "Failed to translate the input stream to a JSON document";
        }

        public static string httpInterfaceInstance()
        {
            return "Failed to instanciate the HTTP interface";
        }

        public static string FullSyncInterfaceInstance()
        {
            return "Failed to instanciate the FullSync interface";
        }

        public static string getDocumentFromDatabase(string documentId)
        {
            return "Failed to get fullSync document '" + documentId + "' from the database";
        }

        internal static string FullSyncReplicationFail(string databaseName, string way)
        {
            return "Failed to '" + way + "' replicate the '" + databaseName + "' database";
        }

        public static string localCachePolicyIsDisable()
        {
            return "Depending to the network state the local cache is disabled";
        }

        public static string localCacheDocumentJustCreated()
        {
            return "The local cache document is just created (empty)";
        }

        public static string illegalArgumentInvalidLocalCachePolicy(string localCachePolicyString)
        {
            return "The local cache policy '" + localCachePolicyString + "' is invalid";
        }

        public static string timeToLiveExpired()
        {
            return "The time to live expired";
        }

        public static string InvalidLocalCacheResponseInformation()
        {
            return "Local cache response informations are invalid";
        }

        public static string overrideDocument()
        {
            return "Failed to override the fullSync document";
        }

        public static string handleFullSyncRequest()
        {
            return "Failed while running the fullSync request";
        }

        public static string serializeC8oCallRequest()
        {
            return "Failes to serialize the Convertigo call request";
        }

        public static string getResponseFromLocalCache()
        {
            return "Failed to get response from the local cache";
        }

        public static string getResponseFromLocalCacheDocument()
        {
            return "Failed to get response form the local cache document";
        }

        public static string handleC8oCallRequest()
        {
            return "Failed while runnig the c8o call request";
        }

        public static string saveResponseToLocalCache()
        {
            return "Failed to save the response to the local cache";
        }

        //	public static string illegalArgumentCallParametersNull() {
        //		return "Call parameters must be not null";
        //	}
        //	
        //	public static string illegalArgumentCallC8oResponseListenerNull() {
        //		return "Call response listener must be not null";
        //	}

        public static string RemoteLogFail()
        {
            return "Failed to send log to the Convertigo server: disabling remote logging";
        }

        public static string FullSyncRequestFail()
        {
            return "Failed to process the fullsync request";
        }

        internal static string MissingLocalCacheResponseDocument()
        {
            return "Missing local cache response document";
        }
    }
}
