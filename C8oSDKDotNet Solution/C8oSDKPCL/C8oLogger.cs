using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using Convertigo.SDK.Listeners;
using Convertigo.SDK.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Convertigo.SDK.Utils;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using Convertigo.SDK.Exceptions;

namespace Convertigo.SDK
{
    public class C8oLogger
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
        private ConcurrentQueue<C8oLog> remoteLogs;
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
        /// Indicates if exceptions thrown when sending logs are handled by the C8oResponseListener.
        /// </summary>
        private Boolean handleExceptionsOnLog;
        /// <summary>
        /// 
        /// </summary>
        private C8oExceptionListener c8oExceptionListener;
        /// <summary>
        /// Used to run HTTP requests.
        /// </summary>
        private HttpInterface httpInterface;
        private String deviceUuid;

        public C8oLogger(C8oExceptionListener defaultC8oExceptionListener, C8oSettings c8oSettings)
        {
            this.c8oExceptionListener = defaultC8oExceptionListener;
            this.handleExceptionsOnLog = false;

            // Remote log
            this.isLogRemote = false;
            this.remoteLogs = new ConcurrentQueue<C8oLog>();
            this.alreadyRemoteLogging = new Boolean[] { false };
            this.remoteLogLevel = C8oLogLevel.NULL;

            DateTime currentTime = DateTime.Now;
            this.startTimeRemoteLog = currentTime;
            this.uidRemoteLogs = C8oTranslator.DoubleToHexString(C8oUtils.GetUnixEpochTime(currentTime));
        }

        private Boolean IsLoggableRemote(C8oLogLevel logLevel)
        {
            return this.isLogRemote && logLevel.priority >= this.remoteLogLevel.priority;
        }

        private Boolean IsLoggableConsole(C8oLogLevel logLevel)
        {
            return true;
        }

        //*** Basics log ***//

        public void Log(C8oLogLevel logLevel, String message)
        {
            this.Log(logLevel, message, this.IsLoggableConsole(logLevel), this.IsLoggableRemote(logLevel));
        }

        public void Log(C8oLogLevel logLevel, String message, Boolean isLoggableConsole, Boolean isLoggableRemote)
        {
            if (isLoggableConsole)
            {
                C8oLogger.LogLocal(logLevel, message);
            }

            if (isLoggableRemote)
            {
                String time = (DateTime.Now.Subtract(this.startTimeRemoteLog)).TotalSeconds + "";
                this.remoteLogs.Enqueue(new C8oLog(time, logLevel, message));
                this.LogRemote();
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
                Boolean canLog = false;
                lock (this.alreadyRemoteLogging)
                {
                    // If there is no another thread already logging AND there is at least one log
                    canLog = !this.alreadyRemoteLogging[0] && (this.remoteLogs.Count > 0);
                    if (canLog)
                    {
                        this.alreadyRemoteLogging[0] = true;
                    }
                }

                if (canLog)
                {
                    // Take logs in the queue and add it to a json array
                    int count = 0;
                    int listSize = this.remoteLogs.Count;
                    JArray logsArray = new JArray();
                    while (count < listSize && count < C8oLogger.REMOTE_LOG_LIMIT)
                    {
                        C8oLog c8oLog = this.remoteLogs.Dequeue();
                        JObject jsonLog = new JObject();
                        jsonLog.Add(C8oLogger.JSON_KEY_TIME, c8oLog.time);
                        jsonLog.Add(C8oLogger.JSON_KEY_LEVEL, c8oLog.logLevel.name);
                        jsonLog.Add(C8oLogger.JSON_KEY_MESSAGE, c8oLog.message);
                        logsArray.Add(jsonLog);
                        count++;
                    }

                    // Initializes request paramters
                    Dictionary<String, Object> parameters = new Dictionary<String, Object>();
                    parameters.Add(C8oLogger.JSON_KEY_LOGS, JsonConvert.SerializeObject(logsArray));
                    parameters.Add(C8oLogger.JSON_KEY_ENV, "{\"uid\":\"" + this.uidRemoteLogs + "\"}");
                    parameters.Add(C8o.ENGINE_PARAMETER_DEVICE_UUID, this.deviceUuid);

                    JObject jsonResponse;
                    try
                    {
                        WebResponse webResponse = await HttpInterface.HandleRequest(this.remoteLogUrl, parameters);
                        Stream streamResponse = webResponse.GetResponseStream();
                        jsonResponse = C8oTranslator.StreamToJson(streamResponse);
                    }
                    catch (Exception e)
                    {
                        this.isLogRemote = false;
                        if (this.handleExceptionsOnLog)
                        {
                            this.c8oExceptionListener.OnException(new C8oException(C8oExceptionMessage.ToDo(), e), null);
                        }
                        return;
                    }

                    JToken logLevelResponse = jsonResponse.GetValue(C8oLogger.JSON_KEY_REMOTE_LOG_LEVEL);
                    if (logLevelResponse != null)
                    {
                        String logLevelResponseStr = (String)logLevelResponse;
                        C8oLogLevel c8oLogLevel = C8oLogLevel.GetC8oLogLevel(logLevelResponseStr);
                        if (c8oLogLevel != null)
                        {
                            this.remoteLogLevel = c8oLogLevel;
                        }
                        this.LogRemote();
                    }
                }
            }).ContinueWith((completedTask) => 
            {
                lock (this.alreadyRemoteLogging)
                {
                    this.alreadyRemoteLogging[0] = false;
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
            Boolean isLoggableConsole = this.IsLoggableConsole(C8oLogLevel.DEBUG);
            Boolean isLoggableRemote = this.IsLoggableRemote(C8oLogLevel.DEBUG);
            if (isLoggableConsole || isLoggableRemote)
            {
                String methodCallLogMessage = "Method call : " + methodName;

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
        public void LogC8oCall(String url, Dictionary<String, String> parameters)
        {
            Boolean isLoggableConsole = this.IsLoggableConsole(C8oLogLevel.DEBUG);
            Boolean isLoggableRemote = this.IsLoggableRemote(C8oLogLevel.DEBUG);

            if (isLoggableConsole || isLoggableRemote)
            {
                String c8oCallLogMessage = "C8o call :" +
                    " URL=" + url + ", " +
                    " Parameters=" + parameters;

                this.Log(C8oLogLevel.DEBUG, c8oCallLogMessage, isLoggableConsole, isLoggableRemote);
            }
        }

        /// <summary>
        /// Log the c8o call XML response in TRACE level.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        public void LogC8oCallXMLResponse(XDocument response, String url, Dictionary<String, String> parameters)
        {
            this.LogC8oCallResponse(C8oTranslator.XmlToString(response), "XML", url, parameters);
        }

        /// <summary>
        /// Log the c8o call JSON response in TRACE level.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        public void LogC8oCallJSONResponse(JObject response, String url, Dictionary<String, String> parameters)
        {
            this.LogC8oCallResponse(C8oTranslator.JsonToString(response), "JSON", url, parameters);
        }

        private void LogC8oCallResponse(String responseStr, String responseType, String url, Dictionary<String, String> parameters)
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

        public void SetRemoteLogParameters(HttpInterface httpInterface, Boolean isLogRemote, String urlBase, String deviceUuid)
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
