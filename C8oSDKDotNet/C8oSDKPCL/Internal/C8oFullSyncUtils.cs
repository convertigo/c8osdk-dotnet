using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Convertigo.SDK.Internal
{
    internal class FullSyncUtils
    {
        //*** Post merge ***//

        public static void MergeProperties(IDictionary<string, object> newProperties, IDictionary<string, object> oldProperties)
        {
            // Iterates on each old properties
            IEnumerator<KeyValuePair<string, object>> oldPropertiesEnumerator = oldProperties.GetEnumerator();
            while (oldPropertiesEnumerator.MoveNext())
            {
                // Gets old document key and value
                KeyValuePair<string, object> oldProperty = oldPropertiesEnumerator.Current;
                String oldPropertyKey = oldProperty.Key;
                Object oldPropertyValue = oldProperty.Value;

                // Checks if there is a new value to merge with the old one (if the new document contains the same key)
                Object newPropertyValue;
                if (newProperties.TryGetValue(oldPropertyKey, out newPropertyValue))
                {
                    if (newPropertyValue is IDictionary<string, object> && oldPropertyValue is IDictionary<string, object>)
                    {
                        FullSyncUtils.MergeProperties(newPropertyValue as Dictionary<string, object>, oldPropertyValue as Dictionary<string, object>);
                    }
                    else if (newPropertyValue is JObject && oldPropertyValue is JObject)
                    {
                        FullSyncUtils.MergeProperties(newPropertyValue as JObject, oldPropertyValue as JObject);
                    }
                    else if (newPropertyValue is JArray && oldPropertyValue is JArray)
                    {
                        FullSyncUtils.MergeArrayProperties(newPropertyValue as JArray, oldPropertyValue as JArray);
                    }
                    else
                    {
                        // If the new document has the same key but its value is not the same type than the old one or if their type are "simple"
                        // Does nothing cause the right value is the new one
                    }
                }
                else
                {
                    // If the new document does not contain the key then adds it
                    newProperties.Add(oldPropertyKey, oldPropertyValue);
                }
            }
        }

        private static void MergeProperties(JObject newProperties, JObject oldProperties)
        {
            // Iterates on each old properties
            var oldPropertiesEnumerable = oldProperties.Properties();
            foreach (var oldProperty in oldPropertiesEnumerable)
            {
                // Gets old document key and value
                string oldPropertyKey = oldProperty.Name;
                var oldPropertyValue = oldProperty.Value;

                // Checks if there is a new value to merge with the old one (if the new document contains the same key)
                JToken newPropertyValue;
                if (newProperties.TryGetValue(oldPropertyKey, out newPropertyValue))
                {
                    // Get the new document value
                    //Object newPropertyValue;
                    // newProperties.TryGetValue(oldPropertyKey, out newPropertyValue);
                    if (newPropertyValue is JObject && oldPropertyValue is JObject)
                    {
                        FullSyncUtils.MergeProperties(newPropertyValue as JObject, oldPropertyValue as JObject);
                    }
                    else if (newPropertyValue is JArray && oldPropertyValue is JArray)
                    {
                        FullSyncUtils.MergeArrayProperties(newPropertyValue as JArray, oldPropertyValue as JArray);
                    }
                    else
                    {
                        // If the new document has the same key but its value is not the same type than the old one or if their type are "simple"
                        // Does nothing cause the right value is the new one
                    }
                }
                else
                {
                    // If the new document does not contain the key then adds it
                    newProperties.Add(oldPropertyKey, oldPropertyValue);
                }
            }
        }

        private static void MergeArrayProperties(JArray newArray, JArray oldArray)
        {
            int newArraySize = newArray.Count;
            int oldArraySize = oldArray.Count;

            // Iterates on old values
            for (int i = 0; i < oldArraySize; i++)
            {
                // Gets new and old values at this index
                JToken newArrayValue = null;
                if (i < newArraySize)
                {
                    newArrayValue = newArray[i];
                }
                JToken oldArrayValue = oldArray[i];

                // If there is a new value to merge with the old one
                if (newArrayValue != null)
                {
                    if (newArrayValue is JObject && oldArrayValue is JObject)
                    {
                        FullSyncUtils.MergeProperties(newArrayValue as JObject, oldArrayValue as JObject);
                    }
                    else if (newArrayValue is JArray && oldArrayValue is JArray)
                    {
                        FullSyncUtils.MergeArrayProperties(newArrayValue as JArray, oldArrayValue as JArray);
                    }
                    else
                    {
                        // If the new document has the same key but its value is not the same type than the old one or if their type are "simple"
                        // Does nothing cause the right value is the new one
                    }
                }
                else
                {
                    newArray.Insert(i, oldArrayValue);
                }
            }
        }

    }


}
