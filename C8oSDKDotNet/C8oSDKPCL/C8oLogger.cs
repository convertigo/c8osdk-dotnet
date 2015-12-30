using Convertigo.SDK.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Convertigo.SDK
{
    public class C8oLogger
    {
        private static readonly Regex RE_FORMAT_TIME = new Regex("(\\d*?)(?:,|.)(\\d{3}).*");
        //*** Constants ***//

        /// <summary>
        /// The log tag used by the SDK.
        /// </summary>
        private static readonly string LOG_TAG = "c8o";
        private static readonly string LOG_INTERNAL_PREFIX = "[" + LOG_TAG + "] ";
        /// <summary>
        /// The maximum number of logs sent to the Convertigo server in one time.
        /// </summary>
        private static readonly int REMOTE_LOG_LIMIT = 100;
        /// <summary>
        /// Convertigo log levels.
        /// </summary>
        // private static String[] REMOTE_LOG_LEVELS = { "", "none", "trace", "debug", "info", "warn", "error", "fatal" };

        private static readonly string JSON_KEY_REMOTE_LOG_LEVEL = "remoteLogLevel";
        private static readonly string JSON_KEY_TIME = "time";
        private static readonly string JSON_KEY_LEVEL = "level";
        private static readonly string JSON_KEY_MESSAGE = "msg";
        private static readonly string JSON_KEY_LOGS = "logs";
        private static readonly string JSON_KEY_ENV = "env";

        //*** Attributes ***//

        /// <summary>
        /// Indicates if logs are sent to the Convertigo server. 
        /// </summary>
        private bool isLogRemote;
        /// <summary>
        /// The URL used to send logs.
        /// </summary>
        private string remoteLogUrl;
        /// <summary>
        /// Contains logs to be sent to the Convertigo server.
        /// </summary>
        private Queue<JObject> remoteLogs;
        /// <summary>
        /// Indicates if a thread is sending logs.
        /// </summary>
        private bool[] alreadyRemoteLogging;
        /// <summary>
        /// The log level returned by the Convertigo server.
        /// </summary>
        private C8oLogLevel remoteLogLevel;
        /// <summary>
        /// The UID sent to the Convertigo server.
        /// </summary>
        private string uidRemoteLogs;
        /// <summary>
        /// The date in milliseconds at the creation of the C8o instance.
        /// </summary>
        private DateTime startTimeRemoteLog;
        private C8o c8o;

        internal C8oLogger(C8o c8o)
        {
            this.c8o = c8o;

            remoteLogUrl = c8o.EndpointConvertigo + "/admin/services/logs.Add";
            remoteLogs = new Queue<JObject>();
            alreadyRemoteLogging = new bool[] { false };

            isLogRemote = c8o.LogRemote;
            remoteLogLevel = C8oLogLevel.TRACE;

            var currentTime = DateTime.Now;
            startTimeRemoteLog = currentTime;
            uidRemoteLogs = C8oTranslator.DoubleToHexString(C8oUtils.GetUnixEpochTime(currentTime));
        }

        private bool IsLoggableRemote(C8oLogLevel logLevel)
        {
            return isLogRemote && logLevel != null && C8oLogLevel.TRACE.priority <= remoteLogLevel.priority && remoteLogLevel.priority <= logLevel.priority;
        }

        private bool IsLoggableConsole(C8oLogLevel logLevel)
        {
            return logLevel != null && C8oLogLevel.TRACE.priority <= c8o.LogLevelLocal.priority && c8o.LogLevelLocal.priority <= logLevel.priority;
        }

        //*** Basics log ***//
        public bool CanLog(C8oLogLevel logLevel)
        {
            return IsLoggableConsole(logLevel) || IsLoggableRemote(logLevel);
        }

        public bool IsFatal
        {
            get { return CanLog(C8oLogLevel.FATAL); }
        }

        public bool IsError
        {
            get { return CanLog(C8oLogLevel.ERROR); }
        }

        public bool IsWarn
        {
            get { return CanLog(C8oLogLevel.WARN); }
        }

        public bool IsInfo
        {
            get { return CanLog(C8oLogLevel.INFO); }
        }

        public bool IsDebug
        {
            get { return CanLog(C8oLogLevel.DEBUG); }
        }

        public bool IsTrace
        {
            get { return CanLog(C8oLogLevel.TRACE); }
        }

        internal void Log(C8oLogLevel logLevel, string message, Exception exception = null)
        {
            bool isLogConsole = IsLoggableConsole(logLevel);
            bool isLogRemote = IsLoggableRemote(logLevel);

            if (isLogConsole || isLogRemote)
            {
                if (exception != null)
                {
                    message += "\n" + exception;
                }

                string time = (DateTime.Now.Subtract(startTimeRemoteLog).TotalSeconds + "").Replace(",", ".");
                time = RE_FORMAT_TIME.Replace(time, "$1.$2");

                if (isLogRemote)
                {
                    remoteLogs.Enqueue(new JObject()
                    {
                        { JSON_KEY_TIME, time},
                        { JSON_KEY_LEVEL, logLevel.name},
                        { JSON_KEY_MESSAGE, message }
                    });
                    LogRemote();
                }

                if (isLogConsole)
                {
                    System.Diagnostics.Debug.WriteLine("(" + time + ") [" + logLevel.name + "] " + message);
                }
            }
        }

        public void Fatal(string message, Exception exception = null)
        {
            Log(C8oLogLevel.FATAL, message, exception);
        }

        public void Error(string message, Exception exception = null)
        {
            Log(C8oLogLevel.ERROR, message, exception);
        }

        public void Warn(string message, Exception exception = null)
        {
            Log(C8oLogLevel.WARN, message, exception);
        }

        public void Info(string message, Exception exception = null)
        {
            Log(C8oLogLevel.INFO, message, exception);
        }

        public void Debug(string message, Exception exception = null)
        {
            Log(C8oLogLevel.DEBUG, message, exception);
        }

        public void Trace(string message, Exception exception = null)
        {
            Log(C8oLogLevel.TRACE, message, exception);
        }

        internal void _Log(C8oLogLevel logLevel, string message, Exception exception = null)
        {
            if (c8o.LogC8o)
            {
                Log(logLevel, LOG_INTERNAL_PREFIX + message, exception);
            }
        }

        internal void _Fatal(string message, Exception exception = null)
        {
            _Log(C8oLogLevel.FATAL, message, exception);
        }

        internal void _Error(string message, Exception exception = null)
        {
            _Log(C8oLogLevel.ERROR, message, exception);
        }

        internal void _Warn(string message, Exception exception = null)
        {
            _Log(C8oLogLevel.WARN, message, exception);
        }

        internal void _Info(string message, Exception exception = null)
        {
            _Log(C8oLogLevel.INFO, message, exception);
        }

        internal void _Debug(string message, Exception exception = null)
        {
            _Log(C8oLogLevel.DEBUG, message, exception);
        }

        internal void _Trace(string message, Exception exception = null)
        {
            _Log(C8oLogLevel.TRACE, message, exception);
        }

        private void LogRemote()
        {
            bool canLog = false;
            lock (alreadyRemoteLogging)
            {
                // If there is no another thread already logging AND there is at least one log
                canLog = !alreadyRemoteLogging[0] && remoteLogs.Count > 0;
                if (canLog)
                {
                    alreadyRemoteLogging[0] = true;
                }
            }

            if (canLog)
            {
                Task.Run(async () =>
                {
                    // Take logs in the queue and add it to a json array
                    int count = 0;
                    int listSize = remoteLogs.Count;
                    var logsArray = new JArray();

                    while (count < listSize && count < REMOTE_LOG_LIMIT)
                    {
                        logsArray.Add(remoteLogs.Dequeue());
                        count++;
                    }

                    // Initializes request paramters
                    var parameters = new Dictionary<string, object>()
                    {
                        { JSON_KEY_LOGS, JsonConvert.SerializeObject(logsArray)},
                        { JSON_KEY_ENV, "{\"uid\":\"" + uidRemoteLogs + "\"}"},
                        { C8o.ENGINE_PARAMETER_DEVICE_UUID, c8o.DeviceUUID }
                    };

                    JObject jsonResponse;
                    try
                    {
                        var webResponse = await c8o.httpInterface.HandleRequest(remoteLogUrl, parameters);
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
                        string logLevelResponseStr = logLevelResponse.Value<string>();
                        var c8oLogLevel = C8oLogLevel.GetC8oLogLevel(logLevelResponseStr);

                        if (c8oLogLevel != null)
                        {
                            remoteLogLevel = c8oLogLevel;
                        }
                        LogRemote();
                    }
                }).ContinueWith((completedTask) => 
                {
                    lock (alreadyRemoteLogging)
                    {
                        alreadyRemoteLogging[0] = false;
                    }
                });
            }
        }

        //*** Others log ***// 

        /// <summary>
        /// Log the method call in DEBUG log level and log method parameters in VERBOSE log level.
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="parameters">Array containing method parameters</param>
        internal void LogMethodCall(string methodName, params object[] parameters)
        {
            if (c8o.LogC8o && IsDebug)
            {
                string methodCallLogMessage = "Method call : " + methodName;

                if (IsTrace)
                {
                    methodCallLogMessage += ", Parameters : [";
                    // Add parameters
                    foreach (Object parameter in parameters)
                    {
                        methodCallLogMessage += "\n" + parameter + ", ";
                    }
                    // Remove the last character
                    methodCallLogMessage = methodCallLogMessage.Substring(0, methodCallLogMessage.Length - 2) + "]";

                    _Trace(methodCallLogMessage);
                }
                else
                {
                    _Debug(methodCallLogMessage);
                }
            }
        }

        /// <summary>
        /// Log the c8o call in DEBUG log level.
        /// </summary>
        /// <param name="url">The c8o call URL</param>
        /// <param name="parameters">c8o call parameters</param>
        internal void LogC8oCall(string url, IDictionary<string, object> parameters)
        {
            if (c8o.LogC8o && IsDebug)
            {
                string c8oCallLogMessage = "C8o call :" +
                    " URL=" + url + ", " +
                    " Parameters=" + parameters;

                _Debug(c8oCallLogMessage);
            }
        }

        /// <summary>
        /// Log the c8o call XML response in TRACE level.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        internal void LogC8oCallXMLResponse(XDocument response, string url, IDictionary<string, object> parameters)
        {
            LogC8oCallResponse(C8oTranslator.XmlToString(response), "XML", url, parameters);
        }

        /// <summary>
        /// Log the c8o call JSON response in TRACE level.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        internal void LogC8oCallJSONResponse(JObject response, string url, IDictionary<string, object> parameters)
        {
            LogC8oCallResponse(C8oTranslator.JsonToString(response), "JSON", url, parameters);
        }

        private void LogC8oCallResponse(string responseStr, string responseType, string url, IDictionary<string, object> parameters)
        {
            if (c8o.LogC8o && IsTrace)
            {
                string c8oCallResponseLogMessage = "C8o call " + responseType + " response :" +
                    " URL=" + url + ", " +
                    " Parameters=" + parameters + ", " +
                    " Response=" + responseStr;

                _Trace(c8oCallResponseLogMessage);
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

        internal string name;
        internal int priority;

        private C8oLogLevel(string name, int priority)
        {
            this.name = name;
            this.priority = priority;
        }

        internal static C8oLogLevel GetC8oLogLevel(string name)
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
