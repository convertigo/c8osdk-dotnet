using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Convertigo.SDK
{
    public class FullSyncTranslator
    {
        public static readonly String FULL_SYNC_RESPONSE_KEY_COUNT = "count";
        public static readonly String FULL_SYNC_RESPONSE_KEY_ROWS = "rows";
        public static readonly String FULL_SYNC_RESPONSE_KEY_CURRENT = "current";
        public static readonly String FULL_SYNC_RESPONSE_KEY_DIRECTION = "direction";
        public static readonly String FULL_SYNC_RESPONSE_KEY_TOTAL = "total";
        public static readonly String FULL_SYNC_RESPONSE_KEY_OK = "ok";

        public static readonly String FULL_SYNC_RESPONSE_VALUE_DIRECTION_PUSH = "push";
        public static readonly String FULL_SYNC_RESPONSE_VALUE_DIRECTION_PULL = "pull";

        public static readonly String XML_KEY_DOCUMENT = "document";
        public static readonly String XML_KEY_COUCHDB_OUTPUT = "couchdb_output";

        public static XDocument FullSyncJsonToXml(JObject json)
        {
            XDocument xmlDocument = new XDocument();
            // Create the root element node
            XElement rootElement = new XElement(XML_KEY_DOCUMENT);
            xmlDocument.Add(rootElement);
            XElement couchdb_output = new XElement(XML_KEY_COUCHDB_OUTPUT);

            // Translates the JSON document
             C8oTranslator.JsonToXml(json, couchdb_output);
            rootElement.Add(couchdb_output);
            return xmlDocument;
        }

        public static JObject DictionaryToJson(IDictionary<String, Object> dictionary)
        {

            String jsonStr = JsonConvert.SerializeObject(dictionary);
            JObject json = JObject.Parse(jsonStr);
            return json;
        }

        public static String DictionaryToString(IDictionary<String, Object> dict)
        {
            String str = "{ ";

            foreach (KeyValuePair<String, Object> item in dict)
            {
                String valueStr;
                if (item.Value is IDictionary<String, Object>)
                {
                    valueStr = DictionaryToString(item.Value as IDictionary<String, Object>);
                }
                else if (item.Value is IList<Object>)
                {
                    valueStr = ListToString(item.Value as IList<Object>);
                }
                else
                {
                    valueStr = item.Value.ToString();
                }


                str += item.Key + " : " + valueStr + ", ";
            }

            if (dict.Count > 0)
            {
                str = str.Remove(str.Length - 2);
            }

            str += " }";

            return str;
        }

        public static String ListToString(IList<Object> list)
        {
            String str = "[";
            foreach (Object item in list)
            {
                str = str + item.ToString() + ", ";
            }

            if (list.Count > 0)
            {
                str = str.Remove(str.Length - 2);
            }

            str = str + "]";

            return str;
        }

    }
}
