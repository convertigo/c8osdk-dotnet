using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Couchbase.Lite;
using Convertigo.SDK;
using Convertigo.SDK.Utils;
using Convertigo.SDK.FullSync.Enums;
using System.Net;
using Convertigo.SDK.Exceptions;
using System.Threading.Tasks;

namespace Convertigo.SDK.FullSync
{
    public class C8oFullSyncDatabase
    {
        //*** Constants ***//

        /// <summary>
        /// The name of the authentication cookie.
        /// </summary>
        public static string AUTHENTICATION_COOKIE_NAME = "SyncGatewaySession";

        private C8o c8o;

        //*** Attributes ***//

        private string databaseName;
        private Database database = null;
        private FullSyncReplication pullFullSyncReplication;
        private FullSyncReplication pushFullSyncReplication;

        public C8oFullSyncDatabase(C8o c8o, Manager manager, string databaseName, string fullSyncDatabases, string localSuffix)
        {
            this.c8o = c8o;

            string C8oFullSyncDatabaseUrl = fullSyncDatabases + databaseName + "/";

            this.databaseName = (databaseName += localSuffix);

            try
            {
                database = manager.GetDatabase(databaseName);
                for (int i = 0; i < 6 && database == null; i++)
                {
                    Task.Delay(500).Wait();
                    database = manager.GetDatabase(databaseName);
                }
                if (database == null)
                {
                    throw new C8oException("Cannot get the local database: " + databaseName);
                }
            }
            catch (Exception e)
            {
                if (!(e is C8oException))
                {
                    e = new C8oException(C8oExceptionMessage.ToDo(), e);
                }
                throw e;
            }

            // The "/" at the end is important
            Uri C8oFullSyncDatabaseUri = new Uri(C8oFullSyncDatabaseUrl);

            Replication pullReplication = database.CreatePullReplication(C8oFullSyncDatabaseUri);
            Replication pushReplication = database.CreatePushReplication(C8oFullSyncDatabaseUri);

            // ??? Does surely something but do not know what, it is optional so it is still here ???
            String authenticationCookieValue = c8o.AuthenticationCookieValue;
            if (authenticationCookieValue != null)
            {
                DateTime expirationDate = DateTime.Now;
                expirationDate.AddDays(1);

                Boolean isSecure = false;
                Boolean httpOnly = false;

                pullReplication.SetCookie(C8oFullSyncDatabase.AUTHENTICATION_COOKIE_NAME, authenticationCookieValue, "/", expirationDate, isSecure, httpOnly);
                pushReplication.SetCookie(C8oFullSyncDatabase.AUTHENTICATION_COOKIE_NAME, authenticationCookieValue, "/", expirationDate, isSecure, httpOnly);
            }

            pullFullSyncReplication = new FullSyncReplication(pullReplication);
            pushFullSyncReplication = new FullSyncReplication(pushReplication);
        }

        //*** Replication ***//

        public void StartAllReplications(IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            StartPullReplication(parameters, c8oResponseListener);
            StartPushReplication(parameters, c8oResponseListener);
        }

        public void StartPullReplication(IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            StartReplication(pullFullSyncReplication, parameters, c8oResponseListener);
        }

        public void StartPushReplication(IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
        {
            StartReplication(pushFullSyncReplication, parameters, c8oResponseListener);
        }

        private void StartReplication(FullSyncReplication fullSyncReplication, IDictionary<string, object> parameters, C8oResponseListener c8oResponseListener)
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
                    string parameterValue = C8oUtils.GetParameterStringValue(parameters, fullSyncReplicateDatabaseParameter.name, false);
                    if (parameterValue != null)
                    {
                        // Cancel the replication
                        if (fullSyncReplicateDatabaseParameter == FullSyncReplicationParameter.CANCEL && parameterValue.Equals("true"))
                        {
                            return;
                        }
                        try
                        {
                            C8oFullSyncCblEnum.SetReplication(fullSyncReplicateDatabaseParameter, fullSyncReplication.replication, parameterValue);
                        }
                        catch (Exception e)
                        {
                            throw new C8oException(C8oExceptionMessage.ToDo(), e);
                        }
                    }
                }

                // Cookies
                var cookies = c8o.CookieStore.GetCookies(new Uri(c8o.Endpoint));
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
                    //Type c8oResponseListenerType = listener.GetType();
                    if (c8oResponseListener is C8oResponseCblListener)
                    {
                        fullSyncReplication.changeListener = new EventHandler<ReplicationChangeEventArgs>((source, replicationChangeEventArgs) =>
                        {
                            ((C8oResponseCblListener)c8oResponseListener).OnReplicationChangeEventResponse(replicationChangeEventArgs, parameters);
                        });
                    }
                    else if (c8oResponseListener is C8oResponseJsonListener)
                    {
                        fullSyncReplication.changeListener = new EventHandler<ReplicationChangeEventArgs>((source, replicationChangeEventArgs) =>
                        {
                            ((C8oResponseJsonListener)c8oResponseListener).OnJsonResponse(C8oFullSyncCblTranslator.ReplicationChangeEventArgsToJson(replicationChangeEventArgs), parameters);
                        });
                    }
                    else if (c8oResponseListener is C8oResponseXmlListener)
                    {

                        fullSyncReplication.changeListener = new EventHandler<ReplicationChangeEventArgs>((source, replicationChangeEventArgs) =>
                        {
                            ((C8oResponseXmlListener)c8oResponseListener).OnXmlResponse(C8oFullSyncCblTranslator.ReplicationChangeEventArgsToXml(replicationChangeEventArgs), parameters);
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
            DestroyReplication(pullFullSyncReplication);
            pullFullSyncReplication = null;
            DestroyReplication(pushFullSyncReplication);
            pushFullSyncReplication = null;
        }

        private static void DestroyReplication(FullSyncReplication fullSyncReplication)
        {
            if (fullSyncReplication.replication != null)
            {
                fullSyncReplication.replication.Stop();
                fullSyncReplication.replication.DeleteCookie(C8oFullSyncDatabase.AUTHENTICATION_COOKIE_NAME);
                fullSyncReplication.replication = null;
            }
        }

        //*** Getter / Setter ***//

        public string DatabaseName
        {
            get { return databaseName; }
        }

        public Database Database
        {
            get { return database; }
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
            changeListener = null;
        }
    }
}
