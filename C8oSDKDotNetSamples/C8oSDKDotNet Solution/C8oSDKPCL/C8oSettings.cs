using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Convertigo.SDK.FullSync;
using Newtonsoft.Json; 

namespace Convertigo.SDK
{
    public class C8oSettings
    {

        //*** HTTP ***//

        /// <summary>
        /// The connection timeout to Convertigo in milliseconds. A value of zero means the timeout is not used
        /// </summary>
        internal int timeout;
        /// <summary>
        /// Indicate if https calls trust all certificates or not
        /// </summary>
        internal bool trustAllCetificates;
        /// <summary>
        /// Cookies to send to the Convertigo server.
        /// </summary>
        internal CookieCollection cookies;

        //*** Log ***//

        /// <summary>
        /// Indicate if logs are sent to the Convertigo server.
        /// Default is true.
        /// </summary>
        internal Boolean isLogRemote;
        internal Boolean handleExceptionsOnLog;

        //*** FullSync ***//

        internal String defaultFullSyncDatabaseName;
        internal String authenticationCookieValue;

        internal String fullSyncServerUrl;
        internal String fullSyncUsername;
        internal String fullSyncPassword;
        internal String fullSyncLocalSuffix;

        //*** Properties ***//

        public String AuthenticationCookieValue
        {
            get
            {
                return this.authenticationCookieValue;
            }
        }

        //*** Constructor ***//

        public C8oSettings()
        {
            //*** HTTP ***//
            this.timeout = -1;
            this.trustAllCetificates = false;
            this.cookies = null;
            //*** Log ***//
            this.isLogRemote = true;
            this.handleExceptionsOnLog = false;
            //*** FullSync ***//
            this.defaultFullSyncDatabaseName = null;
            this.authenticationCookieValue = null;
            this.fullSyncServerUrl = "http://localhost:5984";
            this.fullSyncUsername = null;
            this.fullSyncPassword = null;
            this.fullSyncLocalSuffix = null;
        }

        public override string ToString() 
        { 
            String returnString = this.GetType().ToString() + "[";

            returnString += "timeout = " + timeout + ", " +
				    "trustAllCetificates = " + trustAllCetificates + 
                    "]";

            return returnString;
        }

        //*** Getter / Setter ***//

        //*** HTTP ***//

        public C8oSettings SetTimeout(int timeout)
        {
            if (timeout <= 0)
            {
                timeout = -1;
            }
            this.timeout = timeout;
            return this;
        }

        public C8oSettings SetTrustAllCertificates(bool trustAllCetificates)
        {
            this.trustAllCetificates = trustAllCetificates;
            return this;
        }

        public C8oSettings AddCookie(String name, String value)
        {
            if (this.cookies == null)
            {
                this.cookies = new CookieCollection();
            }
            this.cookies.Add(new Cookie(name, value));

            return this;
        }

        //*** Log ***//

        public C8oSettings SetIsLogRemote(Boolean isLogRemote)
        {
            this.isLogRemote = isLogRemote;
            return this;
        }

        public C8oSettings SetHandleExceptionsOnLog(Boolean handleExceptionsOnLog)
        {
            this.handleExceptionsOnLog = handleExceptionsOnLog;
            return this;
        }
        //*** FullSync ***//

        public C8oSettings SetDefaultFullSyncDatabaseName(String defaultFullSyncDatabaseName)
        {
            this.defaultFullSyncDatabaseName = defaultFullSyncDatabaseName;
            return this;
        }

        public C8oSettings SetAuthenticationCookieValue(String authenticationCookieValue)
        {
            this.authenticationCookieValue = authenticationCookieValue;
            return this;
        }

        public C8oSettings SetFullSyncServerUrl(String fullSyncServerUrl)
        {
            this.fullSyncServerUrl = fullSyncServerUrl;
            return this;
        }

        public C8oSettings SetFullSyncUsername(String fullSyncUsername)
        {
            this.fullSyncUsername = fullSyncUsername;
            return this;
        }

        public C8oSettings SetFullSyncPassword(String fullSyncPassword)
        {
            this.fullSyncPassword = fullSyncPassword;
            return this;
        }

        public C8oSettings SetFullSyncLocalSuffix(String fullSyncLocalSuffix)
        {
            this.fullSyncLocalSuffix = fullSyncLocalSuffix;
            return this;
        }

        public C8oSettings Clone()
        {
            C8oSettings clone = new C8oSettings();
            clone.timeout = timeout;
            clone.trustAllCetificates = trustAllCetificates;
            clone.cookies = cookies;
            clone.isLogRemote = isLogRemote;
            clone.handleExceptionsOnLog = handleExceptionsOnLog;
            clone.defaultFullSyncDatabaseName = defaultFullSyncDatabaseName;
            clone.authenticationCookieValue = authenticationCookieValue;
            clone.fullSyncServerUrl = fullSyncServerUrl;
            clone.fullSyncUsername = fullSyncUsername;
            clone.fullSyncPassword = fullSyncPassword;
            clone.fullSyncLocalSuffix = fullSyncLocalSuffix;
            return clone;
        }
    }
}
