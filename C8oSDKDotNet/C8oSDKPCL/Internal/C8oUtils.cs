using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Convertigo.SDK.Internal
{
    internal class C8oUtils
    {

        /// <summary>
        /// FullSync parameters prefix.
        /// </summary>
	    private static string USE_PARAMETER_IDENTIFIER = "_use_";


        //*** Class ***//

        public static string GetObjectClassName(object obj)
        {
            string className = "null";
            if (obj != null)
            {
                className = obj.GetType().Name;
            }
            return className;
        }

        //*** Parameters ***//

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="name"></param>
        /// <param name="useName"></param>
        /// <returns></returns>
        public static KeyValuePair<string, object> GetParameter(IDictionary<string, object> parameters, string name, bool useName = false)
        {
            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                string parameterName = parameter.Key;
                if (name.Equals(parameterName) || (useName && name.Equals(C8oUtils.USE_PARAMETER_IDENTIFIER + parameterName)))
                {
                    return parameter;
                }
            }
            return new KeyValuePair<string, object>(null, null);
        }

        public static object GetParameterObjectValue(IDictionary<string, object> parameters, string name, bool useName = false)
        {
            KeyValuePair<string, object> parameter = GetParameter(parameters, name, useName);
            if (parameter.Key != null)
            {
                return parameter.Value;
            }
            return null;
        }

        /// <summary>
        /// Searches in the list the parameter with this specific name (or the same name with the prefix '_use_') and returns it.
	    /// Returns null if the parameter is not found.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="name"></param>
        /// <param name="useName"></param>
        /// <returns></returns>
        public static string GetParameterStringValue(IDictionary<string, object> parameters, string name, bool useName = false)
        {
            var parameter = GetParameter(parameters, name, useName);
            if (parameter.Key != null)
            {
                return "" + parameter.Value;
            }
            return null;
        }

        public static string PeekParameterStringValue(IDictionary<string, object> parameters, string name, bool exceptionIfMissing = false)
        {
            string value = GetParameterStringValue(parameters, name, false);
            if (value == null)
            {
                if (exceptionIfMissing)
                {
                    throw new ArgumentException(C8oExceptionMessage.MissParameter(name));
                }
            }
            else
            {
                parameters.Remove(name);
            }
            return value;
        }

        public static object GetParameterJsonValue(IDictionary<string, object> parameters, string name, bool useName = false) 
        {
		    var parameter = GetParameter(parameters, name, useName);
            if (parameter.Key != null)
            {
                return C8oUtils.GetParameterJsonValue(parameter);
            }
		    return null;
	    }

        public static object GetParameterJsonValue(KeyValuePair<string, object> parameter)
        {
            var obj = parameter.Value;
            if (obj is JValue || obj is string)
            {
                return obj;
            }

            var str = JsonConvert.SerializeObject(obj);
            return C8oTranslator.StringToJson(str);
        }

        public static bool TryGetParameterObjectValue<T>(IDictionary<string, object> parameters, string name, out T value, bool useName = false, T defaultValue = default(T))
        {
            KeyValuePair<string, object> parameter = GetParameter(parameters, name, useName);
            if (parameter.Key != null && parameter.Value != null)
            {
                if (parameter.Value is string && typeof(T) != typeof(string))
                {
                    value = (T) C8oTranslator.StringToObject(parameter.Value as string, typeof(T));
                }
                else
                {
                    value = (T) parameter.Value;
                }
                return true;
            }
            value = defaultValue;
            return false;
        }

        //*** Others ***//

        /// <summary>
        /// Checks if the specified string is an valid URL.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsValidUrl(string url)
        {
            Uri uriResult = null;
            return (Uri.TryCreate(url, UriKind.Absolute, out uriResult) && (uriResult.Scheme == "http" || uriResult.Scheme == "https"));
        }

        /// <summary>
        /// Get the UNIX epoch time of the specified date (number of milliseconds elapsed since 01/01/1970 00:00:00).
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static long GetUnixEpochTime(DateTime date)
        {
            TimeSpan timeSpan = date.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0));
            return (long) timeSpan.TotalMilliseconds;
        }

        //public static T GetParameterAndCheckType<T>(IDictionary<string, object> parameters, String name, T defaultValue = default(T))
        //{
        //    // KeyValuePair<SC8oUtils.GetParameter(parameters, name);

        //    return defaultValue;
        //}

        //public static T GetValueAndCheckType<T>(Dictionary<string, object> jObject, String key, T defaultValue = default(T))
        //{
        //    JToken value;
        //    if (jObject.TryGetValue(key, out value))
        //    {
        //        if (value is T)
        //        {
        //            return value as T;
        //        }
        //        else if (value is JValue && (value as JValue).Value is T)
        //        {
        //            return (value as JValue).Value;
        //        }
        //    }
        //    return defaultValue;
        //}

        public static bool TryGetValueAndCheckType<T>(JObject jObject, string key, out T value)
        {
            JToken foundValue;
            if (jObject.TryGetValue(key, out foundValue))
            {
                if (foundValue is T)
                {
                    value = (T)(object)foundValue;
                    return true;
                }
                else if (foundValue is JValue && (foundValue as JValue).Value is T)
                {
                    value = (T)(object)(foundValue as JValue).Value;
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        public static string IdentifyC8oCallRequest(IDictionary<string, object> parameters, string responseType)
        {
            JObject json = new JObject();
            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                JValue value = new JValue(parameter.Value);
                json.Add(parameter.Key, value);
            }
            return responseType + json.ToString();
        }

        public static string UrlDecode(string str)
        {
            return Uri.UnescapeDataString(str);
        }

    }
}
