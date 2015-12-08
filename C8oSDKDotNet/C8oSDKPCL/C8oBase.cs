using System;
using System.Net;

namespace Convertigo.SDK
{
    public class C8oBase
    {
        //*** HTTP ***//

        protected int timeout = -1;
        protected bool trustAllCetificates = false;
        protected CookieCollection cookies = null;

        //*** Log ***//

        protected bool logRemote = true;
        protected C8oOnFail logOnFail;

        //*** FullSync ***//

        protected string defaultDatabaseName = null;
        protected string authenticationCookieValue = null;
        protected string fullSyncLocalSuffix = null;

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
            if (cookies == null)
            {
                cookies = new CookieCollection();
            }
            if (c8oBase.cookies != null)
            {
                cookies.Add(c8oBase.cookies);
            }
            

            //*** Log ***//

            logRemote = c8oBase.logRemote;
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
