using System;
using System.Collections.Generic;
using System.Net;

namespace Convertigo.SDK
{
    /// <summary>
    /// This class manages various settings to configure Convertigo SDK. You can use an instance of this object in a
    /// new C8o endpoint object to initialize the endpoint with the correct settings
    /// </summary>
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
        /// When using https TLS/SSL connections you may have to provide client certificates. Use this setting to add a client certificate 
        /// that the SDK will use connecting to Convertigo Server.
        /// </summary>
        /// <param name="certificate">A PKCS#12 Binary certificate</param>
        /// <param name="password">the password to use this certificate</param>
        /// <returns>The current <c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings AddClientCertificate(byte[] certificate, string password)
        {
            if (clientCertificateBinaries == null)
            {
                clientCertificateBinaries = new Dictionary<byte[], string>();
            }
            clientCertificateBinaries.Add(certificate, password);

            return this;
        }

        /// <summary>
        /// When using https TLS/SSL connections you may have to provide client certificates. Use this setting to add a client certificate 
        /// that the SDK will use connecting to Convertigo Server.
        /// </summary>
        /// <param name="certificate">The path to a .P12 certificate file</param>
        /// <param name="password">the password to use this certificate</param>
        /// <returns>The current <c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings AddClientCertificate(string certificatePath, string password)
        {
            if (clientCertificateFiles == null)
            {
                clientCertificateFiles = new Dictionary<string, string>();
            }
            clientCertificateFiles.Add(certificatePath, password);

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
            if (cookies == null)
            {
                cookies = new CookieCollection();
            }
            cookies.Add(new Cookie(name, value));

            return this;
        }

        //*** Log ***//

        /// <summary>
        /// Set logging to remote. If true, logs will be sent to Convertigo MBaaS server.
        /// </summary>
        /// <param name="logRemote"></param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetLogRemote(bool logRemote)
        {
            this.logRemote = logRemote;
            return this;
        }

        /// <summary>
        /// Sets a value indicating the log level you want in the device console
        /// You should use C8oLogLevel constants
        /// </summary>
        /// <param name="logLevelLocal"></param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetLogLevelLocal(C8oLogLevel logLevelLocal)
        {
            this.logLevelLocal = logLevelLocal;
            return this;
        }

        public C8oSettings SetLogC8o(bool logC8o)
        {
            this.logC8o = logC8o;
            return this;
        }

        public C8oSettings SetLogOnFail(C8oOnFail logOnFail)
        {
            this.logOnFail = logOnFail;
            return this;
        }
        //*** FullSync ***//

        /// <summary>
        /// When you use FullSync request in the form fs://database.verb, you can use this setting if you want to have a default
        /// database. In this case using fs://.verb will automatically use the database configured with this setting.
        /// </summary>
        /// <param name="defaultDatabaseName">The default data base</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
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

        /// <summary>
        /// Set the storage engine for local FullSync databases. Use C8o.FS_STORAGE_SQL or C8o.FS_STORAGE_FORESTDB.
        /// </summary>
        /// <param name="fullSyncStorageEngine">C8o.FS_STORAGE_SQL or C8o.FS_STORAGE_FORESTDB.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetFullSyncStorageEngine(string fullSyncStorageEngine)
        {
            if (C8o.FS_STORAGE_SQL.Equals(fullSyncStorageEngine) ||
                    C8o.FS_STORAGE_FORESTDB.Equals(fullSyncStorageEngine))
            {
                this.fullSyncStorageEngine = fullSyncStorageEngine;
            }
            return this;
        }

        /// <summary>
        /// Set the encryption key for local FullSync databases encryption.
        /// </summary>
        /// <param name="fullSyncEncryptionKey">The encryption key to use.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetFullSyncEncryptionKey(string fullSyncEncryptionKey)
        {
            this.fullSyncEncryptionKey = fullSyncEncryptionKey;
            return this;
        }

        /// <summary>
        /// Set the FullSync replication heartbeat for long running request (def: 30 secondes).
        /// </summary>
        /// <param name="fullSyncReplicationHeartbeat">Heartbeat duration for FullSync replication.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetFullSyncReplicationHeartbeat(TimeSpan fullSyncReplicationHeartbeat)
        {
            this.fullSyncReplicationHeartbeat = fullSyncReplicationHeartbeat;
            return this;
        }

        /// <summary>
        /// Set the FullSync replication socket timeout (def: 5 minutes).
        /// </summary>
        /// <param name="fullSyncReplicationSocketTimeout">Socket max duration for FullSync replication.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetFullSyncReplicationSocketTimeout(TimeSpan fullSyncReplicationSocketTimeout)
        {
            this.fullSyncReplicationSocketTimeout = fullSyncReplicationSocketTimeout;
            return this;
        }

        /// <summary>
        /// Set the FullSync replication request timeout (def: 5 minutes).
        /// </summary>
        /// <param name="fullSyncReplicationRequestTimeout">Request max duration for FullSync replication.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetFullSyncReplicationRequestTimeout(TimeSpan fullSyncReplicationRequestTimeout)
        {
            this.fullSyncReplicationRequestTimeout = fullSyncReplicationRequestTimeout;
            return this;
        }

        /// <summary>
        /// Set the FullSync replication max open HTTP connections (def: 8).
        /// </summary>
        /// <param name="fullSyncReplicationMaxOpenHttpConnections">Max number of open HTTP connections.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetFullSyncReplicationMaxOpenHttpConnections(int fullSyncReplicationMaxOpenHttpConnections)
        {
            this.fullSyncReplicationMaxOpenHttpConnections = fullSyncReplicationMaxOpenHttpConnections;
            return this;
        }

        /// <summary>
        /// Set the FullSync replication max revision to get in bulk replication.
        /// </summary>
        /// <param name="fullSyncReplicationMaxRevsToGetInBulk">Max number of revision for bulk.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetFullSyncReplicationMaxRevsToGetInBulk(int fullSyncReplicationMaxRevsToGetInBulk)
        {
            this.fullSyncReplicationMaxRevsToGetInBulk = fullSyncReplicationMaxRevsToGetInBulk;
            return this;
        }

        /// <summary>
        /// Set the FullSync replication delay between replication retries.
        /// </summary>
        /// <param name="fullSyncReplicationRetryDelay">Delay between replication retries.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetFullSyncReplicationRetryDelay(TimeSpan fullSyncReplicationRetryDelay)
        {
            this.fullSyncReplicationRetryDelay = fullSyncReplicationRetryDelay;
            return this;
        }

        /// <summary>
        /// Set the encryption key for local FullSync databases encryption.
        /// </summary>
        /// <param name="uiDispatcher">The encryption key to use.</param>
        /// <returns>The current<c>C8oSettings</c>, for chaining.</returns>
        public C8oSettings SetUiDispatcher(Action<Action> uiDispatcher)
        {
            this.uiDispatcher = uiDispatcher;
            return this;
        }
    }
}
