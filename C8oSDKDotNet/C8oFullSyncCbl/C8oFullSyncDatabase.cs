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
                var options = new DatabaseOptions();
                options.Create = true;
                if (c8o.FullSyncEncryptionKey != null)
                {
                    var key = new Couchbase.Lite.Store.SymmetricKey(c8o.FullSyncEncryptionKey);
                    options.EncryptionKey = key;
                }
                if (C8o.FS_STORAGE_SQL.Equals(c8o.FullSyncStorageEngine))
                {
                    options.StorageType = StorageEngineTypes.SQLite;
                }
                else
                {
                    options.StorageType = StorageEngineTypes.ForestDB;
                }

                try
                {
                    c8o.Log._Debug("manager.OpenDatabase(databaseName, options); //create");
                    database = manager.OpenDatabase(databaseName, options);
                    c8o.Log._Debug("manager.OpenDatabase(databaseName, options); //create ok");
                } catch (Exception ex)
                {
                    c8o.Log._Debug("manager.OpenDatabase(databaseName, options); //nocreate");
                    options.Create = false;
                    database = manager.OpenDatabase(databaseName, options);
                    c8o.Log._Debug("manager.OpenDatabase(databaseName, options); //nocreate ok");
                }
                
                /*for (int i = 0; i < 6 && database == null; i++)
                {
                    Task.Delay(500).Wait();
                    database = manager.OpenDatabase(databaseName, options);
                }*/
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
            var cookies = c8o.CookieStore.GetCookies(new Uri(c8o.EndpointConvertigo + '/'));
            foreach (Cookie cookie in cookies)
            {
                replication.SetCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Expires, cookie.Secure, false);
            }

            replication.ReplicationOptions.UseWebSocket = false;
            replication.ReplicationOptions.Heartbeat = c8o.FullSyncReplicationHeartbeat;
            replication.ReplicationOptions.SocketTimeout = c8o.FullSyncReplicationSocketTimeout;
            replication.ReplicationOptions.RequestTimeout = c8o.FullSyncReplicationRequestTimeout;
            replication.ReplicationOptions.MaxOpenHttpConnections = c8o.FullSyncReplicationMaxOpenHttpConnections;
            replication.ReplicationOptions.MaxRevsToGetInBulk = c8o.FullSyncReplicationMaxRevsToGetInBulk;
            replication.ReplicationOptions.ReplicationRetryDelay = c8o.FullSyncReplicationRetryDelay;

            return replication;
        };

        internal void Delete()
        {
            if (database != null)
            {
                try
                {
                    StopReplication(pullFullSyncReplication);
                    StopReplication(pushFullSyncReplication);
                    var manager = database.Manager;
                    database.Delete();
                    manager.ForgetDatabase(database);
                }
                catch (CouchbaseLiteException e)
                {
                    c8o.Log._Info("Failed to close database", e);
                }
                finally
                {
                    database = null;
                }
            }
        }

        private void StopReplication(FullSyncReplication fsReplication)
        {
            if (fsReplication.replication != null)
            {
                StopReplication(fsReplication.replication);
                if (fsReplication.changeListener != null)
                {
                    fsReplication.replication.Changed -= fsReplication.changeListener;
                    fsReplication.changeListener = null;
                }
                fsReplication.replication = null;
            }
        }

        private Replication GetReplication(FullSyncReplication fsReplication)
        {
            StopReplication(fsReplication);
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
            bool continuous;
            bool cancel = false;

            if (parameters.ContainsKey("live"))
            {
                continuous = parameters["live"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            else if (parameters.ContainsKey("continuous"))
            {
                continuous = parameters["continuous"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                continuous = false;
            }

            if (parameters.ContainsKey("cancel"))
            {
                cancel = parameters["cancel"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            var rep = GetReplication(fullSyncReplication);
            var _progress = new C8oProgress();
            _progress.Raw = rep;
            _progress.Pull = rep.IsPull;

            if (cancel)
            {
                StopReplication(fullSyncReplication);
                _progress.Finished = true;

                if (c8oResponseListener != null && c8oResponseListener is C8oResponseProgressListener)
                {
                    (c8oResponseListener as C8oResponseProgressListener).OnProgressResponse(_progress, null);
                }

                return;
            }

            var param = new Dictionary<string, object>(parameters);

            rep.Changed +=
                fullSyncReplication.changeListener =
                new EventHandler<ReplicationChangeEventArgs>((source, changeEvt) =>
                {
                    var progress = _progress;
                    progress.Total = rep.ChangesCount;
                    progress.Current = rep.CompletedChangesCount;
                    progress.TaskInfo = C8oFullSyncTranslator.DictionaryToString(rep.ActiveTaskInfo);
                    progress.Status = "" + rep.Status;
                    progress.Finished = rep.Status != ReplicationStatus.Active;

                    if (progress.Changed)
                    {
                        _progress = new C8oProgress(progress);

                        if (c8oResponseListener != null && c8oResponseListener is C8oResponseProgressListener)
                        {
                            param[C8o.ENGINE_PARAMETER_PROGRESS] = progress;
                            (c8oResponseListener as C8oResponseProgressListener).OnProgressResponse(progress, param);
                        }
                    }

                    if (progress.Finished)
                    {
                        StopReplication(fullSyncReplication);
                        if (continuous)
                        {
                            long lastCurrent = progress.Current;
                            rep = GetReplication(fullSyncReplication);
                            _progress.Raw = rep;
                            _progress.Continuous = true;
                            rep.Continuous = true;
                            rep.Changed +=
                                fullSyncReplication.changeListener =
                                new EventHandler<ReplicationChangeEventArgs>((src, chEvt) =>
                                {
                                    progress = _progress;
                                    progress.Total = rep.ChangesCount;
                                    progress.Current = rep.CompletedChangesCount;
                                    progress.TaskInfo = C8oFullSyncTranslator.DictionaryToString(rep.ActiveTaskInfo);
                                    progress.Status = "" + rep.Status;

                                    //if (progress.Current > lastCurrent && progress.Changed)
                                    if (progress.Changed)
                                    {
                                        _progress = new C8oProgress(progress);

                                        if (c8oResponseListener != null && c8oResponseListener is C8oResponseProgressListener)
                                        {
                                            param[C8o.ENGINE_PARAMETER_PROGRESS] = progress;
                                            (c8oResponseListener as C8oResponseProgressListener).OnProgressResponse(progress, param);
                                        }
                                    }
                                });
                            rep.Start();
                        }
                    }
                });

            rep.Start();
        }

        private void StopReplication(Replication replication)
        {
            var str = "" + replication.ActiveTaskInfo;
            replication.Continuous = false;
            replication.Stop();
            str += "\n" + replication.ActiveTaskInfo;
            
            int retry = 100;

            while (replication.IsRunning && retry-- > 0)
            {
                replication.Stop();
                Thread.Sleep(10);
            }

            str += "\n" + replication.ActiveTaskInfo;
            try
            {
                Replication dbRep = null;
                // prevent the "Not starting because identical puller already exists" bug
                while (dbRep == null && retry-- > 0)
                {
                    foreach (var rep in replication.LocalDatabase.AllReplications)
                    {

                        if (rep == replication)
                        {
                            dbRep = rep;
                            Thread.Sleep(10);
                        }
                    }
                }
            }
            catch
            {
                // ignore 
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
