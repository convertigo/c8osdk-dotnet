using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using UnicodeNormalization;

namespace Convertigo.SDK.Internal
{
    internal class C8oTranslator
    {        
	    private static string XML_KEY_ITEM = "item";
	    private static string XML_KEY_OBJECT = "object";
	    private static string XML_KEY__ATTACHMENTS = "_attachments";
	    private static string XML_KEY_ATTACHMENT = "attachment";
	    private static string XML_KEY_NAME = "name";

        /// <summary>
        /// Translates the specidied JSON to XML and append it to the specidief XML element.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="xmlDocument"></param>
        /// <param name="parentElement"></param>
        public static void JsonToXml(JToken json, XElement parentElement)
        {
            // Translates the JSON object depending to its type
            if (json is JObject)
            {
                var jsonObject = json as JObject;
                // Gets all the elements of the JSON object and sorts them
                string[] keys = new String[jsonObject.Count];
                int index = 0;
                foreach (var jsonChild in jsonObject)
                {
                    keys[index] = jsonChild.Key;
                    index++;
                }
                Array.Sort(keys);

                // Translates each elements of the JSON object
                foreach (var key in keys)
                {
                    var keyValue = jsonObject.GetValue(key);
                    JsonKeyToXml(key, keyValue, parentElement);
                }
            }
            else if (json is JArray)
            {
                var  jsonArray = json as JArray;
                // Translates each items of the JSON array
                foreach (var jsonItem in jsonArray)
                {
                    // Create the XML element
                    var item = new XElement(XML_KEY_ITEM);
                    parentElement.Add(item);
                    JsonToXml(jsonItem, item);
                }
            }
            else if (json is JValue)
            {
                var jsonValue = (JValue) json;
                if (jsonValue.Value != null)
                {
                    parentElement.Value = jsonValue.Value.ToString();
                }
            }
        }

        public static void JsonKeyToXml(string jsonKey, JToken jsonValue, XElement parentElement)
        {
            // Replaces the key if it is not specified
            if (String.IsNullOrEmpty(jsonKey))
            {
                jsonKey = XML_KEY_OBJECT;
            }

            // If the parent node contains attachments (Specific to Couch)
		    // TODO why ???
            if (XML_KEY__ATTACHMENTS.Equals(parentElement.Name))
            {
                // Creates the attachment element and its child elements containing the attachment name
                XElement attachmentElement = new XElement(XML_KEY_ATTACHMENT);
                XElement attachmentNameElement = new XElement(XML_KEY_NAME);
                attachmentNameElement.Value = jsonKey;
                attachmentElement.Add(attachmentNameElement);
                parentElement.Add(attachmentElement);

                // Translates the attachment value (it won't override attachment name element because the attachment value is normally a JSON object)
                JsonToXml(jsonValue, attachmentElement);
            }
            else
            {
                // Creates the XML child element with its normalized name
                string normalizedKey = jsonKey.Normalize();
                var childElement = new XElement(normalizedKey);
                parentElement.Add(childElement);

                // Translates the JSON value
                JsonToXml(jsonValue, childElement);
            }
        }

        //*** XML / JSON / Stream to string ***//

        public static string XmlToString(XDocument xmlDocument)
        {
            return xmlDocument.ToString();
        }

        public static string JsonToString(JObject jsonObject)
        {
            return jsonObject.ToString();
        }

        public static string StreamToString(Stream stream)
        {
            //try
            //{
                // stream.Position = 0;
                // stream.Seek(0, SeekOrigin.Begin);               
                StreamReader streamReader = new StreamReader(stream);
                string tmp = streamReader.ReadToEnd();
                return tmp;
            //}
            //catch (Exception e)
            //{
            //    return "";
            //}
        }

        //*** Stream to XML / JSON ***//

        public static JObject StreamToJson(Stream stream)
        {
            // Converts the Stream to string then string to JObject
            string jsonString = StreamToString(stream);
            var json = StringToJson(jsonString) as JObject;
            return json;
        }

        public static XDocument StreamToXml(Stream stream)
        {
            /*var reader = new StreamReader(stream, Encoding.UTF8);
            var value = reader.ReadToEnd();
            XDocument xml = XDocument.Parse(value);*/
            XDocument xml = XDocument.Load(stream);
            return xml;
        }

        //*** string to XML / JSON / object ***//

        public static XDocument StringToXml(string xmlString)
        {
            return XDocument.Parse(xmlString);
        } 

        public static JToken StringToJson(string jsonValueString)
        {
            object obj = jsonValueString;
            try
            {
                obj = JsonConvert.DeserializeObject(jsonValueString);
            }
            catch
            {
            }
            return obj is JToken ? obj as JToken : new JValue(obj);
        }

        public static object StringToObject(string objectValue, Type type)
        {
            try
            {
                return JsonConvert.DeserializeObject(objectValue as string, type);
            }
            catch (Exception e)
            {
                throw new System.FormatException(C8oExceptionMessage.ParseStringToObject(type), e);
            }  
        }

        //*** Others ***//

        public static string ByteArrayToHexString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        public static string DoubleToHexString(double d)
        {
            byte[] bytes = BitConverter.GetBytes(d);
            return C8oTranslator.ByteArrayToHexString(bytes);
        }

        //*** Unused ***//

        public static byte[] StringToByteArray(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static string ByteArrayToString(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            // Checks if the string length is not even
            int plus = 0;
            if (hex.Length % 2 != 0)
            {
                plus = 1;
            }

            int NumberChars = hex.Length / 2;
            byte[] bytes = new byte[NumberChars + plus];
            StringReader sr = new StringReader(hex);
            for (int i = 0; i < NumberChars; i++)
            {
                bytes[i] = Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            }

            // Add the last character
            if (hex.Length % 2 != 0)
            {
                bytes[bytes.Length - 1] = Convert.ToByte(new string(new char[1] { (char)sr.Read() }), 16);
            }

            return bytes;
        }

        public static JObject DictionaryToJson(IDictionary<string, object> dict)
        {
            var json = new JObject();
            foreach (KeyValuePair<string, object> item in dict)
            {
                var value = new JValue(item.Value);
                json.Add(item.Key, value);
            }
            return json;
        }
    }
}
