using Couchbase.Lite;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Convertigo.SDK.Internal
{
    public class C8oFullSyncDatabase
    {
        //*** Constants ***//

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
                    e = new C8oException(C8oExceptionMessage.FullSyncDatabaseInitFailed(databaseName), e);
                }
                throw e;
            }
        }

        internal static Func<C8o, bool, Database, Uri, Replication> createReplication = (c8o, isPull, database, c8oFullSyncDatabaseUrl) =>
        {
            var replication = isPull ?
                database.CreatePullReplication(c8oFullSyncDatabaseUrl) :
                database.CreatePushReplication(c8oFullSyncDatabaseUrl);

            // Cookies
            var cookies = c8o.CookieStore.GetCookies(new Uri(c8o.Endpoint));
            foreach (Cookie cookie in cookies)
            {
                replication.SetCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Expires, cookie.Secure, false);
            }

            return replication;
        };

        private Replication getReplication(FullSyncReplication fsReplication)
        {
            if (fsReplication.replication != null)
            {
                StopReplication(fsReplication.replication);
                if (fsReplication.changeListener != null)
                {
                    fsReplication.replication.Changed -= fsReplication.changeListener;
                }
            }
            var replication = fsReplication.replication = createReplication(c8o, fsReplication.pull, database, c8oFullSyncDatabaseUrl);

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

            if (cancel)
            {
                if (rep != null)
                {
                    StopReplication(rep);
                }
                return;
            }
            
            var param = new Dictionary<string, object>(parameters);
            var progress = new C8oProgress();
            progress.Raw = rep;
            progress.Pull = rep.IsPull;

            var mutex = new object();

            rep.Changed +=
                fullSyncReplication.changeListener =
                new EventHandler<ReplicationChangeEventArgs>((source, changeEvt) =>
                {
                    progress.Total = rep.ChangesCount;
                    progress.Current = rep.CompletedChangesCount;
                    progress.TaskInfo = C8oFullSyncTranslator.DictionaryToString(rep.ActiveTaskInfo);
                    progress.Status = "" + rep.Status;
                    progress.Finished = !rep.IsRunning;

                    if (progress.Finished || rep.Status != ReplicationStatus.Active)
                    {
                        lock (mutex)
                        {
                            Monitor.Pulse(mutex);
                        }
                    }

                    if (progress.Changed)
                    {
                        var newProgress = progress;
                        progress = new C8oProgress(progress);

                        if (c8oResponseListener != null && c8oResponseListener is C8oResponseProgressListener)
                        {
                            Task.Run(() =>
                            {
                                param[C8o.ENGINE_PARAMETER_PROGRESS] = newProgress;
                                (c8oResponseListener as C8oResponseProgressListener).OnProgressResponse(newProgress, param);
                            });
                        }
                    }

                });

            lock (mutex)
            {
                // Finally starts the replication
                rep.Start();
                Monitor.Wait(mutex);
                StopReplication(rep);
            }
            
            if (continuous)
            {
                long lastCurrent = progress.Current;
                rep = getReplication(fullSyncReplication);
                progress.Raw = rep;
                rep.Continuous = progress.Continuous = true;
                rep.Changed +=
                    fullSyncReplication.changeListener =
                    new EventHandler<ReplicationChangeEventArgs>((source, changeEvt) =>
                    {
                        progress.Total = rep.ChangesCount;
                        progress.Current = rep.CompletedChangesCount;
                        progress.TaskInfo = C8oFullSyncTranslator.DictionaryToString(rep.ActiveTaskInfo);
                        progress.Status = "" + rep.Status;

                        if (progress.Current > lastCurrent && progress.Changed)
                        {
                            var newProgress = progress;
                            progress = new C8oProgress(progress);

                            if (c8oResponseListener != null && c8oResponseListener is C8oResponseProgressListener)
                            {
                                Task.Run(() =>
                                {
                                    param[C8o.ENGINE_PARAMETER_PROGRESS] = newProgress;
                                    (c8oResponseListener as C8oResponseProgressListener).OnProgressResponse(progress, param);
                                });
                            }
                        }
                    });
                rep.Start();
            }
        }

        private void StopReplication(Replication replication)
        {
            replication.Stop();
            int retry = 100;

            // prevent the "Not starting becuse identical puller already exists" bug
            while (retry-- > 0)
            {
                foreach (var rep in replication.LocalDatabase.AllReplications)
                {
                    if (rep == replication)
                    {
                        Thread.Sleep(10);
                    }
                    else
                    {
                        return;
                    }
                }

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
