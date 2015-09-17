using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Convertigo.SDK.FullSync; 

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
        public Boolean isLogRemote;
        public Boolean handleExceptionsOnLog;

        //*** FullSync ***//

        public String defaultFullSyncDatabaseName;
        public String authenticationCookieValue;
        public FullSyncInterface fullSyncInterface;

        //*** Constructor ***//

        public C8oSettings()
        {
            //*** HTTP ***//
            this.timeout = 0;
            this.trustAllCetificates = false;
            this.cookies = null;
            //*** Log ***//
            this.isLogRemote = true;
            this.handleExceptionsOnLog = false;
            //*** FullSync ***//
            this.defaultFullSyncDatabaseName = null;
            this.authenticationCookieValue = null;
            this.fullSyncInterface = null; // new DefaultFullSyncInterface2();
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
        public C8oSettings SetFullSyncInterface(FullSyncInterface fullSyncInterface)
        {
            this.fullSyncInterface = fullSyncInterface;
            return this;
        }
        
    }
}
