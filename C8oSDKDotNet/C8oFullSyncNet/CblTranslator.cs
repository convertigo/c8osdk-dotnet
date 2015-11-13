using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json.Linq;
using Couchbase.Lite;
using Newtonsoft.Json;
using System.Xml.Linq;
using Convertigo.SDK.FullSync.Responses;

namespace Convertigo.SDK.FullSync
{
    /// <summary>
    /// Provides static functions to translate fullSync responses to JSON or XML.
    /// </summary>
    class CblTranslator
    {

        //*** Document ***//

        internal static JObject DocumentToJson(Document document)
        {
            JObject json = FullSyncTranslator.DictionaryToJson(document.Properties);
            return json;
        }

        internal static XDocument DocumentToXml(Document document)
        {
            JObject json = DocumentToJson(document);
            XDocument xml = FullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }

        //*** FullSyncDocumentOperationResponse ***//
        //*** DeleteDoucment, PostDocument ***//

        internal static JObject FullSyncDocumentOperationResponseToJSON(FullSyncDocumentOperationResponse fullSyncDocumentOperationResponse)
        {
            JObject json = FullSyncTranslator.DictionaryToJson(fullSyncDocumentOperationResponse.GetProperties());
            return json;
        }

        internal static XDocument FullSyncDocumentOperationResponseToXml(FullSyncDocumentOperationResponse fullSyncDocumentOperationResponse)
        {
            JObject json = FullSyncDocumentOperationResponseToJSON(fullSyncDocumentOperationResponse);
            XDocument xml = FullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }

        //*** QueryEnumerator ***//
        //*** GetAllDocuments ***//

        internal static JObject QueryEnumeratorToJson(QueryEnumerator queryEnumerator) 
        {
            JObject json = new JObject();
            JArray rowsArray = new JArray();

            IEnumerator<QueryRow> queryRows = queryEnumerator.GetEnumerator();
            while (queryRows.MoveNext())
            {
                QueryRow queryRow = queryRows.Current;
                JObject queryRowJson = FullSyncTranslator.DictionaryToJson(queryRow.AsJSONDictionary());
                rowsArray.Add(queryRowJson);
            }

            json.Add(FullSyncTranslator.FULL_SYNC_RESPONSE_KEY_COUNT, queryEnumerator.Count);
            json.Add(FullSyncTranslator.FULL_SYNC_RESPONSE_KEY_ROWS, rowsArray);

            return json;
        }

        internal static XDocument QueryEnumeratorToXml(QueryEnumerator queryEnumerator)
        {
            JObject json = QueryEnumeratorToJson(queryEnumerator);
            XDocument xml = FullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }

        //*** DefaultResponse ***//
        //*** Sync, ReplicatePull, ReplicatePush, Reset ***//

        internal static JObject FullSyncDefaultResponseToJson(FullSyncDefaultResponse fullSyncDefaultResponse)
        {
            JObject json = FullSyncTranslator.DictionaryToJson(fullSyncDefaultResponse.GetProperties());
            return json;
        }

        internal static XDocument FullSyncDefaultResponseToXml(FullSyncDefaultResponse fullSyncDefaultResponse)
        {
            JObject json = FullSyncDefaultResponseToJson(fullSyncDefaultResponse);
            XDocument xml = FullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }

        //*** ChangeEvent ***//
        //*** Sync, ReplicatePull, ReplicatePush ***//

        internal static JObject ReplicationChangeEventArgsToJson(ReplicationChangeEventArgs changeEvent)
        {
            JObject json = new JObject();

            // Change count (total)
            json.Add(FullSyncTranslator.FULL_SYNC_RESPONSE_KEY_TOTAL, changeEvent.Source.ChangesCount);
            // Completed change count (current)
            json.Add(FullSyncTranslator.FULL_SYNC_RESPONSE_KEY_CURRENT, changeEvent.Source.CompletedChangesCount);
            // Progress 
            // ???
            // Direction
            if (changeEvent.Source.IsPull)
            {
                json.Add(FullSyncTranslator.FULL_SYNC_RESPONSE_KEY_DIRECTION, FullSyncTranslator.FULL_SYNC_RESPONSE_VALUE_DIRECTION_PULL);
            }
            else
            {
                json.Add(FullSyncTranslator.FULL_SYNC_RESPONSE_KEY_DIRECTION, FullSyncTranslator.FULL_SYNC_RESPONSE_VALUE_DIRECTION_PUSH);
            }
            // Status (ok)
            if (changeEvent.Source.LastError == null)
            {
                json.Add(FullSyncTranslator.FULL_SYNC_RESPONSE_KEY_OK, true);
            }
            else
            {
                json.Add(FullSyncTranslator.FULL_SYNC_RESPONSE_KEY_OK, false);
            }

            if (changeEvent.Source.DocIds != null)
            {
                json.Add("docids", "" + changeEvent.Source.DocIds.ToString());
            }

            json.Add("taskInfo", "" + FullSyncTranslator.DictionaryToString(changeEvent.Source.ActiveTaskInfo));
            json.Add("status", "" + changeEvent.Source.Status);
            

            return json;
        }

        internal static XDocument ReplicationChangeEventArgsToXml(ReplicationChangeEventArgs changeEvent)
        {
            JObject json = ReplicationChangeEventArgsToJson(changeEvent);
            String toto = json.ToString();
            XDocument xml = FullSyncTranslator.FullSyncJsonToXml(json);
            return xml;
        }
    }
}