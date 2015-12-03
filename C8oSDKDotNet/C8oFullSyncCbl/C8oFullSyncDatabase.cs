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
using System.Threading;

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
        private Uri c8oFullSyncDatabaseUrl;
        private Database database = null;
        private FullSyncReplication pullFullSyncReplication = new FullSyncReplication(true);
        private FullSyncReplication pushFullSyncReplication = new FullSyncReplication(false);



        public C8oFullSyncDatabase(C8o c8o, Manager manager, string databaseName, string fullSyncDatabases, string localSuffix)
        {
            this.c8o = c8o;

            c8oFullSyncDatabaseUrl = new Uri(fullSyncDatabases + databaseName + "/");

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

            /*
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
            */
        }

        private Replication getReplication(FullSyncReplication fsReplication)
        {
            if (fsReplication.replication != null)
            {
                fsReplication.replication.Stop();
                if (fsReplication.changeListener != null)
                {
                    fsReplication.replication.Changed -= fsReplication.changeListener;
                }
            }
            var replication = fsReplication.replication = fsReplication.pull ?
                database.CreatePullReplication(c8oFullSyncDatabaseUrl) :
                database.CreatePushReplication(c8oFullSyncDatabaseUrl);

            // Cookies
            var cookies = c8o.CookieStore.GetCookies(new Uri(c8o.Endpoint));
            foreach (Cookie cookie in cookies)
            {
                replication.SetCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Expires, cookie.Secure, false);
            }

            return replication;
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
            bool continuous = false;
            bool cancel = false;

            if (parameters.ContainsKey("continuous"))
            {
                continuous = parameters["continuous"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            if (parameters.ContainsKey("cancel"))
            {
                cancel = parameters["cancel"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            var rep = getReplication(fullSyncReplication);

            // Cancel the replication if it is already running
            if (rep != null)
            {
                rep.Stop();
            }

            if (cancel)
            {
                return;
            }
 
            /*
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
            */
            
            var param = new Dictionary<string, object>(parameters);
            var progress = new C8oProgress();
            progress.raw = rep;
            progress.pull = rep.IsPull;
            param[C8o.ENGINE_PARAMETER_PROGRESS] = progress;

            var mutex = new object();

            rep.Changed +=
                fullSyncReplication.changeListener =
                new EventHandler<ReplicationChangeEventArgs>((source, changeEvt) =>
                {
                    Task.Run(() =>
                    {
                        progress.total = rep.ChangesCount;
                        progress.current = rep.CompletedChangesCount;
                        progress.taskInfo = C8oFullSyncTranslator.DictionaryToString(rep.ActiveTaskInfo);
                        progress.status = "" + rep.Status;
                        progress.finished = !rep.IsRunning;

                        if (progress.finished)
                        {
                            lock (mutex)
                            {
                                Monitor.Pulse(mutex);
                            }
                        }

                        if (c8oResponseListener != null && c8oResponseListener is C8oResponseProgressListener)
                        {
                            (c8oResponseListener as C8oResponseProgressListener).OnProgressResponse(progress, param);
                        }
                    });
                });

            lock (mutex)
            {
                // Finally starts the replication
                rep.Start();
                Monitor.Wait(mutex);
                rep.Stop();
            }
            
            if (continuous)
            {
                rep = getReplication(fullSyncReplication);
                rep.Continuous = true;
                rep.Start();
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
        public bool pull;

        public FullSyncReplication(bool pull)
        {
            this.pull = pull;
        }
    }
}
