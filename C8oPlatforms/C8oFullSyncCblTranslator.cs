using Couchbase.Lite;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace Convertigo.SDK.Internal
{
    /// <summary>
    /// Provides static functions to translate fullSync responses to JSON or XML.
    /// </summary>
    class C8oFullSyncCblTranslator
    {

        //*** Document ***//

        internal static JObject DocumentToJson(Document document)
        {
            var json = C8oFullSyncTranslator.DictionaryToJson(document.Properties);
            return json;
        }

        internal static XDocument DocumentToXml(Document document)
        {
            var json = DocumentToJson(document);
            var xml = C8oFullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }

        //*** FullSyncDocumentOperationResponse ***//
        //*** DeleteDoucment, PostDocument ***//

        internal static JObject FullSyncDocumentOperationResponseToJSON(FullSyncDocumentOperationResponse fullSyncDocumentOperationResponse)
        {
            var json = C8oFullSyncTranslator.DictionaryToJson(fullSyncDocumentOperationResponse.GetProperties());
            return json;
        }

        internal static XDocument FullSyncDocumentOperationResponseToXml(FullSyncDocumentOperationResponse fullSyncDocumentOperationResponse)
        {
            var json = FullSyncDocumentOperationResponseToJSON(fullSyncDocumentOperationResponse);
            var xml = C8oFullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }

        //*** QueryEnumerator ***//
        //*** GetAllDocuments ***//

        internal static JObject QueryEnumeratorToJson(QueryEnumerator queryEnumerator) 
        {
            var json = new JObject();
            var rowsArray = new JArray();

            var queryRows = queryEnumerator.GetEnumerator();
            while (queryRows.MoveNext())
            {
                var queryRow = queryRows.Current;
                var queryRowJson = C8oFullSyncTranslator.DictionaryToJson(queryRow.AsJSONDictionary());
                rowsArray.Add(queryRowJson);
            }

            json[C8oFullSyncTranslator.FULL_SYNC_RESPONSE_KEY_COUNT] = queryEnumerator.Count;
            json[C8oFullSyncTranslator.FULL_SYNC_RESPONSE_KEY_ROWS] = rowsArray;

            return json;
        }

        internal static XDocument QueryEnumeratorToXml(QueryEnumerator queryEnumerator)
        {
            var json = QueryEnumeratorToJson(queryEnumerator);
            XDocument xml = C8oFullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }

        //*** DefaultResponse ***//
        //*** Sync, ReplicatePull, ReplicatePush, Reset ***//

        internal static JObject FullSyncDefaultResponseToJson(FullSyncDefaultResponse fullSyncDefaultResponse)
        {
            var json = C8oFullSyncTranslator.DictionaryToJson(fullSyncDefaultResponse.GetProperties());
            return json;
        }

        internal static XDocument FullSyncDefaultResponseToXml(FullSyncDefaultResponse fullSyncDefaultResponse)
        {
            var json = FullSyncDefaultResponseToJson(fullSyncDefaultResponse);
            XDocument xml = C8oFullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }

        //*** ChangeEvent ***//
        //*** Sync, ReplicatePull, ReplicatePush ***//

        internal static JObject ReplicationChangeEventArgsToJson(ReplicationChangeEventArgs changeEvent)
        {
            var json = new JObject();

            // Change count (total)
            json[C8oFullSyncTranslator.FULL_SYNC_RESPONSE_KEY_TOTAL] = changeEvent.Source.ChangesCount;
            // Completed change count (current)
            json[C8oFullSyncTranslator.FULL_SYNC_RESPONSE_KEY_CURRENT] = changeEvent.Source.CompletedChangesCount;
            // Progress 
            // ???
            // Direction
            if (changeEvent.Source.IsPull)
            {
                json[C8oFullSyncTranslator.FULL_SYNC_RESPONSE_KEY_DIRECTION] = C8oFullSyncTranslator.FULL_SYNC_RESPONSE_VALUE_DIRECTION_PULL;
            }
            else
            {
                json[C8oFullSyncTranslator.FULL_SYNC_RESPONSE_KEY_DIRECTION] = C8oFullSyncTranslator.FULL_SYNC_RESPONSE_VALUE_DIRECTION_PUSH;
            }
            // Status (ok)
            if (changeEvent.Source.LastError == null)
            {
                json[C8oFullSyncTranslator.FULL_SYNC_RESPONSE_KEY_OK] = true;
            }
            else
            {
                json[C8oFullSyncTranslator.FULL_SYNC_RESPONSE_KEY_OK] = false;
            }

            if (changeEvent.Source.DocIds != null)
            {
                json["docids"] = changeEvent.Source.DocIds.ToString();
            }

            json["taskInfo"] = C8oFullSyncTranslator.DictionaryToString(changeEvent.Source.ActiveTaskInfo);
            json["status"] = "" + changeEvent.Source.Status;
            

            return json;
        }

        internal static XDocument ReplicationChangeEventArgsToXml(ReplicationChangeEventArgs changeEvent)
        {
            var json = ReplicationChangeEventArgsToJson(changeEvent);
            string toto = json.ToString();
            var xml = C8oFullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }
    }
}