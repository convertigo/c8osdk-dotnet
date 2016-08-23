using Convertigo.SDK.Internal;
using System;
using System.Collections.Generic;
using System.Net;

namespace Convertigo.SDK
{
    public class C8oBase
    {
        //*** HTTP ***//

        protected int timeout = -1;
        protected bool trustAllCetificates = false;
        protected CookieCollection cookies = null;
        protected Dictionary<byte[], string> clientCertificateBinaries = null;
        protected Dictionary<string, string> clientCertificateFiles = null;

        //*** Log ***//

        protected bool logRemote = true;
        protected C8oLogLevel logLevelLocal = C8oLogLevel.NONE;
        protected bool logC8o = true;
        protected C8oOnFail logOnFail;

        //*** FullSync ***//

        protected string defaultDatabaseName = null;
        protected string authenticationCookieValue = null;
        protected string fullSyncLocalSuffix = null;
        protected string fullSyncStorageEngine = C8o.FS_STORAGE_SQL;
        protected string fullSyncEncryptionKey = null;

        protected string fullSyncServerUrl = "http://localhost:5984";
        protected string fullSyncUsername = null;
        protected string fullSyncPassword = null;

        //*** Getter ***//

        protected Action<Action> uiDispatcher = null;

        /// <summary>
        /// Gets the connection timeout to Convertigo in milliseconds. A value of zero means the timeout is not used.
        /// Default is <c>0</c>.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        public int Timeout
        {
            get { return timeout; }
        }

        /// <summary>
        /// Gets a value indicating whether https calls trust all certificates or not.
        /// Default is <c>false</c>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if https calls trust all certificates; otherwise, <c>false</c>.
        /// </value>
        public bool TrustAllCetificates
        {
            get { return trustAllCetificates; }
        }

        /// <summary>
        /// Gets initial cookies to send to the Convertigo server.
        /// Default is <c>null</c>.
        /// </summary>
        /// <value>
        /// A collection of cookies.
        /// </value>
        public CookieCollection Cookies
        {
            get { return cookies; }
        }

        public IReadOnlyDictionary<byte[], string> ClientCertificateBinaries
        {
            get { return clientCertificateBinaries; }
        }

        public IReadOnlyDictionary<string, string> ClientCertificateFiles
        {
            get { return clientCertificateFiles; }
        }

        /// <summary>
        /// Gets a value indicating if logs are sent to the Convertigo server.
        /// </summary>
        /// <value>
        ///   <c>true</c> if logs are sent to the Convertigo server; otherwise, <c>false</c>.
        /// </value>
        public bool LogRemote
        {
            get { return logRemote; }
        }

        /// <summary>
        /// Sets a value indicating the log level you want in the device console.
        /// </summary>
        /// <value>
        ///   <c>true</c> if logs are sent to the Convertigo server; otherwise, <c>false</c>.
        /// </value>
        public C8oLogLevel LogLevelLocal
        {
            get { return logLevelLocal; }
        }

        public bool LogC8o
        {
            get { return logC8o; }
        }

        public C8oOnFail LogOnFail
        {
            get { return logOnFail; }
        }

        public string DefaultDatabaseName
        {
            get { return defaultDatabaseName; }
        }

        public string AuthenticationCookieValue
        {
            get { return authenticationCookieValue; }
        }

        public string FullSyncLocalSuffix
        {
            get { return fullSyncLocalSuffix; }
        }

        public string FullSyncStorageEngine
        {
            get { return fullSyncStorageEngine; }
        }

        public string FullSyncEncryptionKey
        {
            get { return fullSyncEncryptionKey; }
        }

        public string FullSyncServerUrl
        {
            get { return fullSyncServerUrl; }
        }

        public string FullSyncUsername
        {
            get { return fullSyncUsername; }
        }

        public string FullSyncPassword
        {
            get { return fullSyncPassword; }
        }
        
        public Action<Action> UiDispatcher
        {
            get { return uiDispatcher; }
        }

        protected void Copy(C8oBase c8oBase)
        {
            //*** HTTP ***//

            timeout = c8oBase.timeout;
            trustAllCetificates = c8oBase.trustAllCetificates;

            if (c8oBase.cookies != null)
            {
                if (cookies == null)
                {
                    cookies = new CookieCollection();
                }
                cookies.Add(c8oBase.cookies);
            }

            if (c8oBase.clientCertificateBinaries != null)
            {
                if (clientCertificateBinaries == null)
                {
                    clientCertificateBinaries = new Dictionary<byte[], string>(c8oBase.clientCertificateBinaries);
                }
                else
                {
                    foreach (var entry in c8oBase.clientCertificateBinaries)
                    {
                        clientCertificateBinaries.Add(entry.Key, entry.Value);
                    }
                }
            }

            if (c8oBase.clientCertificateFiles != null)
            {
                if (clientCertificateFiles == null)
                {
                    clientCertificateFiles = new Dictionary<string, string>(c8oBase.clientCertificateFiles);
                }
                else
                {
                    foreach (var entry in c8oBase.clientCertificateFiles)
                    {
                        clientCertificateFiles.Add(entry.Key, entry.Value);
                    }
                }
            }

            //*** Log ***//

            logRemote = c8oBase.logRemote;
            logLevelLocal = c8oBase.logLevelLocal;
            logC8o = c8oBase.logC8o;
            logOnFail = c8oBase.logOnFail;

            //*** FullSync ***//

            defaultDatabaseName = c8oBase.defaultDatabaseName;
            authenticationCookieValue = c8oBase.authenticationCookieValue;
            fullSyncLocalSuffix = c8oBase.fullSyncLocalSuffix;

            fullSyncServerUrl = c8oBase.fullSyncServerUrl;
            fullSyncUsername = c8oBase.fullSyncUsername;
            fullSyncPassword = c8oBase.fullSyncPassword;

            uiDispatcher = c8oBase.uiDispatcher;
        }
    }
}
