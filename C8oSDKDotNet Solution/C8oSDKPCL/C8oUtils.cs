using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Convertigo.SDK.Exceptions;
using Newtonsoft.Json.Linq;

namespace Convertigo.SDK.Utils
{
    public class C8oUtils
    {

        /// <summary>
        /// FullSync parameters prefix.
        /// </summary>
	    private static String USE_PARAMETER_IDENTIFIER = "_use_";

        //*** Class ***//

        public static String GetObjectClassName(Object obj)
        {
            String className = "null";
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
        public static KeyValuePair<String, Object> GetParameter(IDictionary<String, Object> parameters, String name, Boolean useName = false)
        {
            foreach (KeyValuePair<String, Object> parameter in parameters)
            {
                String parameterName = parameter.Key;
                if (name.Equals(parameterName) || (useName && name.Equals(C8oUtils.USE_PARAMETER_IDENTIFIER + parameterName)))
                {
                    return parameter;
                }
            }
            return new KeyValuePair<String, Object>(null, null);
        }

        /// <summary>
        /// Searches in the list the parameter with this specific name (or the same name with the prefix '_use_') and returns it.
	    /// Returns null if the parameter is not found.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="name"></param>
        /// <param name="useName"></param>
        /// <returns></returns>
        public static String GetParameterStringValue(IDictionary<String, Object> parameters, String name, Boolean useName = false)
        {
            KeyValuePair<String, Object> parameter = C8oUtils.GetParameter(parameters, name, useName);
            if (parameter.Key != null)
            {
                return "" + parameter.Value;
            }
            return null;
        }

        public static String PeekParameterStringValue(IDictionary<String, Object> parameters, String name, Boolean exceptionIfMissing = false)
        {
            String value = GetParameterStringValue(parameters, name, false);
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

        public static Object GetParameterJsonValue(IDictionary<String, Object> parameters, String name, Boolean useName = false) 
        {
		    KeyValuePair<String, Object> parameter = C8oUtils.GetParameter(parameters, name, useName);
            if (parameter.Key != null)
            {
                return C8oUtils.GetParameterJsonValue(parameter);
            }
		    return null;
	    }

        public static Object GetParameterJsonValue(KeyValuePair<String, Object> parameter)
        {
            if (parameter.Value is String)
            {
                try
                {
                    return C8oTranslator.StringToJson(parameter.Value as String);
                }
                catch (FormatException e)
                {
                    throw new C8oException(C8oExceptionMessage.GetParameterJsonValue(parameter), e);
                }
            }
            else
            {
                return parameter.Value;
            }
        }

        public static Boolean TryGetParameterObjectValue<T>(IDictionary<String, Object> parameters, String name, out T value, Boolean useName = false, T defaultValue = default(T))
        {
            KeyValuePair<String, Object> parameter = C8oUtils.GetParameter(parameters, name, useName);
            if (parameter.Key != null && parameter.Value != null)
            {
                if (parameter.Value is String)
                {
                    value = (T) C8oTranslator.StringToObject(parameter.Value as String, typeof(T));
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
        public static bool IsValidUrl(String url)
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

        //public static T GetParameterAndCheckType<T>(IDictionary<String, Object> parameters, String name, T defaultValue = default(T))
        //{
        //    // KeyValuePair<SC8oUtils.GetParameter(parameters, name);

        //    return defaultValue;
        //}

        //public static T GetValueAndCheckType<T>(JObject jObject, String key, T defaultValue = default(T))
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

        public static Boolean TryGetValueAndCheckType<T>(JObject jObject, String key, out T value)
        {
            JToken foundValue;
            if (jObject.TryGetValue(key, out foundValue))
            {
                if (foundValue is T)
                {
                    value = (T)(Object)foundValue;
                    return true;
                }
                else if (foundValue is JValue && (foundValue as JValue).Value is T)
                {
                    value = (T)(Object)(foundValue as JValue).Value;
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        public static String IdentifyC8oCallRequest(IDictionary<String, Object> parameters, String responseType)
        {
            JObject json = new JObject();
            foreach (KeyValuePair<String, Object> parameter in parameters)
            {
                JValue value = new JValue(parameter.Value);
                json.Add(parameter.Key, value);
            }
            return responseType + json.ToString();
        }

    }
}
