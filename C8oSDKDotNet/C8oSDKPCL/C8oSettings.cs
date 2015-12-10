using System;
using System.Net;

namespace Convertigo.SDK
{
    public class C8oSettings : C8oBase
    {
        public C8oSettings()
        {

        }

        public C8oSettings(C8oBase c8oSettings)
        {
            Copy(c8oSettings);
        }

        public C8oSettings Clone()
        {
            return new C8oSettings(this);
        }

        /// <summary>
        /// Sets the connection timeout to Convertigo in milliseconds. A value of zero means the timeout is not used.
        /// Default is <c>0</c>.
        /// </summary>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <returns>The current <c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetTimeout(int timeout)
        {
            if (timeout <= 0)
            {
                timeout = -1;
            }
            this.timeout = timeout;
            return this;
        }

        /// <summary>
        /// Gets a value indicating whether https calls trust all certificates or not.
        /// Default is <c>false</c>.
        /// </summary>
        /// <param name="trustAllCetificates">
        ///   <c>true</c> if https calls trust all certificates; otherwise, <c>false</c>.
        /// </param>
        /// <returns>The current <c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetTrustAllCertificates(bool trustAllCetificates)
        {
            this.trustAllCetificates = trustAllCetificates;
            return this;
        }

        /// <summary>
        /// Add a new cookie to the initial cookies send to the Convertigo server.
        /// </summary>
        /// <param name="name">
        /// The name of the new cookie.
        /// </param>
        /// <param name="value">
        /// The value of the new cookie.
        /// </param>
        /// <returns>The current <c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings AddCookie(string name, string value)
        {
            if (this.cookies == null)
            {
                this.cookies = new CookieCollection();
            }
            this.cookies.Add(new Cookie(name, value));

            return this;
        }

        //*** Log ***//

        public C8oSettings SetIsLogRemote(bool logRemote)
        {
            this.logRemote = logRemote;
            return this;
        }

        public C8oSettings SetLogOnFail(C8oOnFail logOnFail)
        {
            this.logOnFail = logOnFail;
            return this;
        }
        //*** FullSync ***//

        public C8oSettings SetDefaultDatabaseName(string defaultDatabaseName)
        {
            this.defaultDatabaseName = defaultDatabaseName;
            return this;
        }

        public C8oSettings SetAuthenticationCookieValue(string authenticationCookieValue)
        {
            this.authenticationCookieValue = authenticationCookieValue;
            return this;
        }

        public C8oSettings SetFullSyncServerUrl(string fullSyncServerUrl)
        {
            this.fullSyncServerUrl = fullSyncServerUrl;
            return this;
        }

        public C8oSettings SetFullSyncUsername(string fullSyncUsername)
        {
            this.fullSyncUsername = fullSyncUsername;
            return this;
        }

        public C8oSettings SetFullSyncPassword(string fullSyncPassword)
        {
            this.fullSyncPassword = fullSyncPassword;
            return this;
        }

        public C8oSettings SetFullSyncLocalSuffix(string fullSyncLocalSuffix)
        {
            this.fullSyncLocalSuffix = fullSyncLocalSuffix;
            return this;
        }

        public C8oSettings SetUiDispatcher(Action<Action> uiDispatcher)
        {
            this.uiDispatcher = uiDispatcher;
            return this;
        }
    }
}
