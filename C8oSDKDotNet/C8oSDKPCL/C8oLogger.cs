using Convertigo.SDK.Exceptions;
using Convertigo.SDK.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Convertigo.SDK
{
    internal class C8oLogger
    {

        //*** Constants ***//

        /// <summary>
        /// The log tag used by the SDK.
        /// </summary>
        private static String LOG_TAG = "c8o";
        /// <summary>
        /// The maximum number of logs sent to the Convertigo server in one time.
        /// </summary>
        private static int REMOTE_LOG_LIMIT = 100;
        /// <summary>
        /// Convertigo log levels.
        /// </summary>
        // private static String[] REMOTE_LOG_LEVELS = { "", "none", "trace", "debug", "info", "warn", "error", "fatal" };

        private static String JSON_KEY_REMOTE_LOG_LEVEL = "remoteLogLevel";
        private static String JSON_KEY_TIME = "time";
        private static String JSON_KEY_LEVEL = "level";
        private static String JSON_KEY_MESSAGE = "msg";
        private static String JSON_KEY_LOGS = "logs";
        private static String JSON_KEY_ENV = "env";

        //*** Attributes ***//

        /// <summary>
        /// Indicates if logs are sent to the Convertigo server. 
        /// </summary>
        private Boolean isLogRemote;
        /// <summary>
        /// The URL used to send logs.
        /// </summary>
        private String remoteLogUrl;
        /// <summary>
        /// Contains logs to be sent to the Convertigo server.
        /// </summary>
        private Queue<C8oLog> remoteLogs;
        /// <summary>
        /// Indicates if a thread is sending logs.
        /// </summary>
        private Boolean[] alreadyRemoteLogging;
        /// <summary>
        /// The log level returned by the Convertigo server.
        /// </summary>
        private C8oLogLevel remoteLogLevel;
        /// <summary>
        /// The UID sent to the Convertigo server.
        /// </summary>
        private String uidRemoteLogs;
        /// <summary>
        /// The date in milliseconds at the creation of the C8o instance.
        /// </summary>
        private DateTime startTimeRemoteLog;
        /// <summary>
        /// Used to run HTTP requests.
        /// </summary>
        private C8oHttpInterface httpInterface;
        private String deviceUuid;
        private C8o c8o;

        public C8oLogger(C8o c8o)
        {
            this.c8o = c8o;

            // Remote log
            isLogRemote = false;
            remoteLogs = new Queue<C8oLog>();
            alreadyRemoteLogging = new Boolean[] { false };
            remoteLogLevel = C8oLogLevel.NULL;

            var currentTime = DateTime.Now;
            startTimeRemoteLog = currentTime;
            uidRemoteLogs = C8oTranslator.DoubleToHexString(C8oUtils.GetUnixEpochTime(currentTime));
        }

        private bool IsLoggableRemote(C8oLogLevel logLevel)
        {
            return isLogRemote && logLevel.priority >= this.remoteLogLevel.priority;
        }

        private bool IsLoggableConsole(C8oLogLevel logLevel)
        {
            return true;
        }

        //*** Basics log ***//

        public void Log(C8oLogLevel logLevel, String message)
        {
            Log(logLevel, message, this.IsLoggableConsole(logLevel), this.IsLoggableRemote(logLevel));
        }

        public void Log(C8oLogLevel logLevel, String message, Boolean isLoggableConsole, Boolean isLoggableRemote)
        {
            if (isLoggableConsole)
            {
                C8oLogger.LogLocal(logLevel, message);
            }

            if (isLoggableRemote)
            {
                string time = (DateTime.Now.Subtract(this.startTimeRemoteLog)).TotalSeconds + "";
                remoteLogs.Enqueue(new C8oLog(time, logLevel, message));
                LogRemote();
            }
        }

        public static void LogLocal(C8oLogLevel logLevel, String message)
        {
            // TMP
            Debug.WriteLine(logLevel.name + " - " + message);
        }

        private void LogRemote()
        {
            Task.Run(async () =>
            {
                bool canLog = false;
                lock (alreadyRemoteLogging)
                {
                    // If there is no another thread already logging AND there is at least one log
                    canLog = !alreadyRemoteLogging[0] && (remoteLogs.Count > 0);
                    if (canLog)
                    {
                        alreadyRemoteLogging[0] = true;
                    }
                }

                if (canLog)
                {
                    // Take logs in the queue and add it to a json array
                    int count = 0;
                    int listSize = this.remoteLogs.Count;
                    var logsArray = new JArray();
                    while (count < listSize && count < REMOTE_LOG_LIMIT)
                    {
                        C8oLog c8oLog = this.remoteLogs.Dequeue();
                        var jsonLog = new JObject();
                        jsonLog.Add(JSON_KEY_TIME, c8oLog.time);
                        jsonLog.Add(JSON_KEY_LEVEL, c8oLog.logLevel.name);
                        jsonLog.Add(JSON_KEY_MESSAGE, c8oLog.message);
                        logsArray.Add(jsonLog);
                        count++;
                    }

                    // Initializes request paramters
                    var parameters = new Dictionary<string, object>();
                    parameters[JSON_KEY_LOGS] = JsonConvert.SerializeObject(logsArray);
                    parameters[JSON_KEY_ENV] = "{\"uid\":\"" + this.uidRemoteLogs + "\"}";
                    parameters[C8o.ENGINE_PARAMETER_DEVICE_UUID] = this.deviceUuid;

                    JObject jsonResponse;
                    try
                    {
                        var webResponse = await httpInterface.HandleRequest(remoteLogUrl, parameters);
                        var streamResponse = webResponse.GetResponseStream();
                        jsonResponse = C8oTranslator.StreamToJson(streamResponse);
                    }
                    catch (Exception e)
                    {
                        isLogRemote = false;
                        if (c8o.LogOnFail != null)
                        {
                            c8o.LogOnFail(new C8oException(C8oExceptionMessage.ToDo(), e), null);
                        }
                        return;
                    }

                    var logLevelResponse = jsonResponse.GetValue(C8oLogger.JSON_KEY_REMOTE_LOG_LEVEL);
                    if (logLevelResponse != null)
                    {
                        string logLevelResponseStr = (String)logLevelResponse;
                        var c8oLogLevel = C8oLogLevel.GetC8oLogLevel(logLevelResponseStr);
                        if (c8oLogLevel != null)
                        {
                            remoteLogLevel = c8oLogLevel;
                        }
                        LogRemote();
                    }
                }
            }).ContinueWith((completedTask) => 
            {
                lock (alreadyRemoteLogging)
                {
                    alreadyRemoteLogging[0] = false;
                }
            });
        }

        //*** Others log ***// 

        /// <summary>
        /// Log the method call in DEBUG log level and log method parameters in VERBOSE log level.
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="parameters">Array containing method parameters</param>
        public void LogMethodCall(String methodName, params Object[] parameters)
        {
            bool isLoggableConsole = IsLoggableConsole(C8oLogLevel.DEBUG);
            bool isLoggableRemote = IsLoggableRemote(C8oLogLevel.DEBUG);

            if (isLoggableConsole || isLoggableRemote)
            {
                string methodCallLogMessage = "Method call : " + methodName;

                isLoggableConsole = this.IsLoggableConsole(C8oLogLevel.TRACE);
                isLoggableRemote = this.IsLoggableRemote(C8oLogLevel.TRACE);

                if (isLoggableConsole || isLoggableRemote)
                {
                    methodCallLogMessage += ", Parameters : [";
                    // Add parameters
                    foreach (Object parameter in parameters)
                    {
                        methodCallLogMessage += "\n" + parameter + ", ";
                    }
                    // Remove the last character
                    methodCallLogMessage = methodCallLogMessage.Substring(0, methodCallLogMessage.Length - 2) + "]";

                    this.Log(C8oLogLevel.TRACE, methodCallLogMessage, isLoggableConsole, isLoggableRemote);
                }
                else
                {
                    this.Log(C8oLogLevel.DEBUG, methodCallLogMessage, isLoggableConsole, isLoggableRemote);
                }
            }
        }

        /// <summary>
        /// Log the c8o call in DEBUG log level.
        /// </summary>
        /// <param name="url">The c8o call URL</param>
        /// <param name="parameters">c8o call parameters</param>
        public void LogC8oCall(string url, IDictionary<string, object> parameters)
        {
            bool isLoggableConsole = IsLoggableConsole(C8oLogLevel.DEBUG);
            bool isLoggableRemote = IsLoggableRemote(C8oLogLevel.DEBUG);

            if (isLoggableConsole || isLoggableRemote)
            {
                string c8oCallLogMessage = "C8o call :" +
                    " URL=" + url + ", " +
                    " Parameters=" + parameters;

                Log(C8oLogLevel.DEBUG, c8oCallLogMessage, isLoggableConsole, isLoggableRemote);
            }
        }

        /// <summary>
        /// Log the c8o call XML response in TRACE level.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        public void LogC8oCallXMLResponse(XDocument response, string url, IDictionary<string, object> parameters)
        {
            LogC8oCallResponse(C8oTranslator.XmlToString(response), "XML", url, parameters);
        }

        /// <summary>
        /// Log the c8o call JSON response in TRACE level.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        public void LogC8oCallJSONResponse(JObject response, string url, IDictionary<string, object> parameters)
        {
            LogC8oCallResponse(C8oTranslator.JsonToString(response), "JSON", url, parameters);
        }

        private void LogC8oCallResponse(string responseStr, string responseType, string url, IDictionary<string, object> parameters)
        {
            Boolean isLoggableConsole = this.IsLoggableConsole(C8oLogLevel.TRACE);
            Boolean isLoggableRemote = this.IsLoggableRemote(C8oLogLevel.TRACE);

            if (isLoggableConsole || isLoggableRemote)
            {
                String c8oCallJSONResponseLogMessage = "C8o call " + responseType + " response :" +
                    " URL=" + url + ", " +
                    " Parameters=" + parameters + ", " +
                    " Response=" + responseStr;

                this.Log(C8oLogLevel.TRACE, c8oCallJSONResponseLogMessage, isLoggableConsole, isLoggableRemote);
            }
        }

        public void SetRemoteLogParameters(C8oHttpInterface httpInterface, bool isLogRemote, string urlBase, string deviceUuid)
        {
            this.httpInterface = httpInterface;
            this.isLogRemote = isLogRemote;
            this.remoteLogUrl = urlBase + "/admin/services/logs.Add";
            this.deviceUuid = deviceUuid;
        }

        private class C8oLog
        {
            /// <summary>
            /// The logged message.
            /// </summary>
            public String message;
            /// <summary>
            /// The log priority level.
            /// </summary>
            public C8oLogLevel logLevel;
            /// <summary>
            /// The time elapsed since the instanciation of the C8o object used to send this log.
            /// </summary>
            public String time;

            public C8oLog(String time, C8oLogLevel logLevel, String message)
            {
                this.time = time;
                this.logLevel = logLevel;
                this.message = message;
            }
        }
    }

    public class C8oLogLevel
    {
        internal static readonly C8oLogLevel NULL = new C8oLogLevel("", 0);
        public static readonly C8oLogLevel NONE = new C8oLogLevel("none", 1);
        public static readonly C8oLogLevel TRACE = new C8oLogLevel("trace", 2);
        public static readonly C8oLogLevel DEBUG = new C8oLogLevel("debug", 3);
        public static readonly C8oLogLevel INFO = new C8oLogLevel("info", 4);
        public static readonly C8oLogLevel WARN = new C8oLogLevel("warn", 5);
        public static readonly C8oLogLevel ERROR = new C8oLogLevel("error", 6);
        public static readonly C8oLogLevel FATAL = new C8oLogLevel("fatal", 7);

        internal static readonly C8oLogLevel[] C8O_LOG_LEVELS = new C8oLogLevel[] { NULL, NONE, TRACE, DEBUG, INFO, WARN, ERROR, FATAL };

        internal String name;
        internal int priority;

        private C8oLogLevel(String name, int priority)
        {
            this.name = name;
            this.priority = priority;
        }

        internal static C8oLogLevel GetC8oLogLevel(String name)
        {
            foreach (C8oLogLevel c8oLogLevel in C8oLogLevel.C8O_LOG_LEVELS)
            {
                if (c8oLogLevel.name.Equals(name))
                {
                    return c8oLogLevel;
                }
            }
            return null;
        }
    }
}
