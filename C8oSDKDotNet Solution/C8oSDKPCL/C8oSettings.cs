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

        public C8oSettings setTimeout(int timeout)
        {
            this.timeout = timeout;

            return this;
        }

        public C8oSettings setTrustAllCertificates(bool trustAllCetificates)
        {
            this.trustAllCetificates = trustAllCetificates;

            return this;
        }

        public C8oSettings addCookie(String name, String value)
        {
            if (this.cookies == null)
            {
                this.cookies = new CookieCollection();
            }
            this.cookies.Add(new Cookie(name, value));

            return this;
        }
    }
}
