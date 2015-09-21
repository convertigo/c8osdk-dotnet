using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Couchbase.Lite;
using Convertigo.SDK.Listeners;
using Convertigo.SDK.Utils;
using Convertigo.SDK.FullSync.Enums;
using System.Net;
using Convertigo.SDK.Exceptions;

namespace Convertigo.SDK.FullSync
{
    public class CblDatabase
    {
        //*** Constants ***//

        /// <summary>
        /// The suffix added to locally replicated databases name.
        /// </summary>
        public static String LOCAL_DATABASE_SUFFIX = "_mobile";
        /// <summary>
        /// The name of the authentication cookie.
        /// </summary>
        public static String AUTHENTICATION_COOKIE_NAME = "SyncGatewaySession";

        private C8o c8o;

        //*** Attributes ***//

        private String databaseName;
        private Database database;
        private FullSyncReplication pullFullSyncReplication;
        private FullSyncReplication pushFullSyncReplication;

        //*** Properties ***//

        public String DatabaseName
        {
            get { return this.databaseName; }
        }

        public CblDatabase(Manager manager, String databaseName, String fullSyncDatabasesUrlStr, C8o c8o)
        {
            this.c8o = c8o;
            this.databaseName = databaseName;
            C8oSettings c8oSettings = c8o.c8oSettings;
            try
            {
                this.database = manager.GetDatabase(databaseName + CblDatabase.LOCAL_DATABASE_SUFFIX);
            }
            catch (Exception e)
            {
                throw new C8oException(C8oExceptionMessage.ToDo(), e);
            }

            // The "/" at the end is important
            String fullSyncDatabaseUrlStr = fullSyncDatabasesUrlStr + this.databaseName + "/";
            Uri fullSyncDatabaseUrl = new Uri(fullSyncDatabaseUrlStr);

            Replication pullReplication = this.database.CreatePullReplication(fullSyncDatabaseUrl);
            Replication pushReplication = this.database.CreatePushReplication(fullSyncDatabaseUrl);

            // ??? Does surely something but do not know what, it is optional so it is still here ???
            String authenticationCookieValue = c8oSettings.AuthenticationCookieValue;
            if (authenticationCookieValue != null)
            {
                DateTime expirationDate = DateTime.Now;
                expirationDate.AddDays(1);

                Boolean isSecure = false;
                Boolean httpOnly = false;

                pullReplication.SetCookie(CblDatabase.AUTHENTICATION_COOKIE_NAME, authenticationCookieValue, "/", expirationDate, isSecure, httpOnly);
                pushReplication.SetCookie(CblDatabase.AUTHENTICATION_COOKIE_NAME, authenticationCookieValue, "/", expirationDate, isSecure, httpOnly);
            }

            this.pullFullSyncReplication = new FullSyncReplication(pullReplication);
            this.pushFullSyncReplication = new FullSyncReplication(pushReplication);
        }

        //*** Replication ***//

        public void StartAllReplications(Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            this.StartPullReplication(parameters, c8oResponseListener);
            this.StartPushReplication(parameters, c8oResponseListener);
        }

        public void StartPullReplication(Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            this.StartReplication(this.pullFullSyncReplication, parameters, c8oResponseListener);
        }

        public void StartPushReplication(Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {
            this.StartReplication(this.pushFullSyncReplication, parameters, c8oResponseListener);
        }

        private void StartReplication(FullSyncReplication fullSyncReplication, Dictionary<String, Object> parameters, C8oResponseListener c8oResponseListener)
        {

            int TMP = this.database.DocumentCount;
            IEnumerable<Replication> reps = database.AllReplications;

            //lock (fullSyncReplication.replication)
            //{
                //Replication replication = fullSyncReplication.replication;
                // Cancel the replication if it is already running
                if (fullSyncReplication.replication.IsRunning)
                {
                    fullSyncReplication.replication.Stop();
                }
 
                // Handles parameters
                foreach (FullSyncReplicationParameter fullSyncReplicateDatabaseParameter in FullSyncReplicationParameter.Values()) 
                {
                    String parameterValue = C8oUtils.GetParameterStringValue(parameters, fullSyncReplicateDatabaseParameter.name, false);
                    if (parameterValue != null)
                    {
                        // Cancel the replication
                        if (fullSyncReplicateDatabaseParameter == FullSyncReplicationParameter.CANCEL && parameterValue.Equals("true"))
                        {
                            return;
                        }
                        try
                        {
                            CblEnum.SetReplication(fullSyncReplicateDatabaseParameter, fullSyncReplication.replication, parameterValue);
                        }
                        catch (Exception e)
                        {
                            throw new C8oException(C8oExceptionMessage.ToDo(), e);
                        }
                    }
                }

                // Cookies
                var cookies = c8o.GetCookies();
                foreach (Cookie cookie in cookies)
                {
                    fullSyncReplication.replication.SetCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Expires, cookie.Secure, false);
                }

                // Removes the current change listener
                if (fullSyncReplication.changeListener != null)
                {
                    fullSyncReplication.replication.Changed -= fullSyncReplication.changeListener;
                }
                // Replaces the change listener by a new one according to the C8oResponseListener type
                fullSyncReplication.changeListener = null;
                if (c8oResponseListener != null)
                {
                    //Type c8oResponseListenerType = c8oResponseListener.GetType();
                    if (c8oResponseListener is C8oCblResponseListener)
                    {
                        fullSyncReplication.changeListener = new EventHandler<ReplicationChangeEventArgs>((source, replicationChangeEventArgs) =>
                        {
                            ((C8oCblResponseListener)c8oResponseListener).OnReplicationChangeEventResponse(replicationChangeEventArgs, parameters);
                        });
                    }
                    else if (c8oResponseListener is C8oJsonResponseListener)
                    {
                        fullSyncReplication.changeListener = new EventHandler<ReplicationChangeEventArgs>((source, replicationChangeEventArgs) =>
                        {
                            ((C8oJsonResponseListener)c8oResponseListener).OnJsonResponse(CblTranslator.ReplicationChangeEventArgsToJson(replicationChangeEventArgs), parameters);
                        });
                    }
                    else if (c8oResponseListener is C8oXmlResponseListener)
                    {

                        fullSyncReplication.changeListener = new EventHandler<ReplicationChangeEventArgs>((source, replicationChangeEventArgs) =>
                        {
                            ((C8oXmlResponseListener)c8oResponseListener).OnXmlResponse(CblTranslator.ReplicationChangeEventArgsToXml(replicationChangeEventArgs), parameters);
                        });
                    }
                    // else error ?
                    if (fullSyncReplication.changeListener != null)
                    {
                        fullSyncReplication.replication.Changed += fullSyncReplication.changeListener;
                    }
                }

                // Finally starts the replication
                fullSyncReplication.replication.Start();
                // fullSyncReplication.replication.Restart();
            //}
        }

        /// <summary>
        /// Stops and destroys pull and push replications.
        /// </summary>
        public void DestroyReplications()
        {
            CblDatabase.DestroyReplication(this.pullFullSyncReplication);
            this.pullFullSyncReplication = null;
            CblDatabase.DestroyReplication(this.pushFullSyncReplication);
            this.pushFullSyncReplication = null;
        }

        private static void DestroyReplication(FullSyncReplication fullSyncReplication)
        {
            if (fullSyncReplication.replication != null)
            {
                fullSyncReplication.replication.Stop();
                fullSyncReplication.replication.DeleteCookie(CblDatabase.AUTHENTICATION_COOKIE_NAME);
                fullSyncReplication.replication = null;
            }
        }

        //*** Getter / Setter ***//

        public Database GetDatabase()
        {
            return this.database;
        }

    }

    //*** Internal classes ***//

    internal class FullSyncReplication
    {
        public Replication replication;
        public EventHandler<ReplicationChangeEventArgs> changeListener;

        public FullSyncReplication(Replication replication)
        {
            this.replication = replication;
            this.changeListener = null;
        }
    }

}