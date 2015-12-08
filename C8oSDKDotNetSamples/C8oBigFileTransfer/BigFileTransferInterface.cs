using Convertigo.SDK;
using Convertigo.SDK.FullSync.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace C8oBigFileTransfer
{
    public class BigFileTransferInterface
    {
        private string endpoint;
        private C8oSettings c8oSettings;
        private FileManager fileManager;
        private C8o c8oTask;
        private bool tasksDbCreated = false;
        private bool alive = true;
        private Dictionary<string, DownloadStatus> tasks = null;
        public event EventHandler<DownloadStatus> RaiseDownloadStatus;
        public event EventHandler<string> RaiseDebug;
        public event EventHandler<Exception> RaiseException;
        
        public BigFileTransferInterface(string endpoint, C8oSettings c8oSettings, FileManager fileManager, string taskDb = "bigfiletransfer_tasks")
        {
            this.endpoint = endpoint;
            this.c8oSettings = c8oSettings.Clone();
            this.fileManager = fileManager;

            c8oTask = new C8o(endpoint, c8oSettings.Clone().SetDefaultDatabaseName(taskDb));
        }

        public void Start()
        {
            if (tasks == null)
            {
                tasks = new Dictionary<string, DownloadStatus>();
                
                Task.Factory.StartNew(async () =>
                {
                    await CheckTaskDb();
                    int skip = 0;

                    var param = new Dictionary<string, object>
                    {
                        {"limit", 1},
                        {"include_docs", true}
                    };

                    while (alive)
                    {
                        try
                        {
                            param["skip"] = skip;
                            var res = await c8oTask.CallJson("fs://.all", param).Async();

                            if ((res["rows"] as JArray).Count > 0)
                            {
                                var task = res["rows"][0]["doc"] as JObject;
                                if (task == null)
                                {
                                    task = await c8oTask.CallJson("fs://.get",
                                        "docid", res["rows"][0]["id"].ToString()
                                    ).Async();
                                }
                                string uuid = task["_id"].ToString();

                                if (!tasks.ContainsKey(uuid))
                                {
                                    string filePath = task["filePath"].Value<string>();

                                    var downloadStatus = tasks[uuid] = new DownloadStatus(uuid, filePath);
                                    Notify(downloadStatus);

                                    DownloadFile(downloadStatus, task).GetAwaiter();

                                    skip = 0;
                                }
                                else
                                {
                                    skip++;
                                }
                            }
                            else
                            {
                                lock (this)
                                {
                                    Monitor.Wait(this);
                                    skip = 0;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            e.ToString();
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        private async Task CheckTaskDb()
        {
            if (!tasksDbCreated)
            {
                await c8oTask.CallJson("fs://.create").Async();
                tasksDbCreated = true;
            }
        }

        public async Task AddFile(string uuid, string filePath)
        {
            await CheckTaskDb();

            await c8oTask.CallJson("fs://.post",
                "_id", uuid,
                "filePath", filePath,
                "replicated", false,
                "assembled", false,
                "remoteDeleted", false
            ).Async();

            lock (this)
            {
                Monitor.Pulse(this);
            }
        }

        public async Task DownloadFile(DownloadStatus downloadStatus, JObject task)
        {
            try
            {
                var c8o = new C8o(endpoint, c8oSettings.Clone().SetFullSyncLocalSuffix("_" + downloadStatus.Uuid));
                bool authenticated = false;

                //
                // 0 : Authenticates the user on the Convertigo server in order to replicate wanted documents
                //
                if (!task["replicated"].Value<bool>() || !task["remoteDeleted"].Value<bool>())
                {
                    var json = await c8o.CallJson(".SelectUuid", "uuid", downloadStatus.Uuid).Async();

                    Debug("SelectUuid:\n" + json.ToString());

                    if (json.SelectToken("document.selected").ToString() != "true")
                    {
                        if (!task["replicated"].Value<bool>())
                        {
                            throw new Exception("uuid not selected");
                        }
                    }
                    else
                    {
                        authenticated = true;
                        downloadStatus.State = DownloadStatus.StateAuthenticated;
                        Notify(downloadStatus);
                    }
                }

                //
                // 1 : Replicate the document discribing the chunks ids list
                //

                if (!task["replicated"].Value<bool>())
                {
                    var locker = new bool[] { false };

                    await c8o.CallJson("fs://.create").Async();

                    c8o.CallJson("fs://.replicate_pull").Then((json, param) =>
                    {
                        lock (locker)
                        {
                                locker[0] = true;
                                Monitor.Pulse(locker);
                        }
                        return null;
                    });

                    downloadStatus.State = DownloadStatus.StateReplicate;
                    Notify(downloadStatus);

                    var allOptions = new Dictionary<string, object> {
                        { "startkey", '"' + downloadStatus.Uuid + "_\"" },
                        { "endkey", '"' + downloadStatus.Uuid + "__\"" }
                    };

                    // Waits the end of the replication if it is not finished
                    while (!locker[0])
                    {
                        
                        try
                        {
                            var all = await c8o.CallJson("fs://.all", allOptions).Async();
                            var rows = all["rows"];
                            if (rows != null)
                            {
                                int current = (rows as JArray).Count;
                                if (current != downloadStatus.Current)
                                {
                                    downloadStatus.Current = current;
                                    Notify(downloadStatus);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug(e.ToString());
                        }
                        
                        lock (locker)
                        {
                           Monitor.Wait(locker, 500);
                        }
                    }

                    if (downloadStatus.Current < downloadStatus.Total)
                    {
                        throw new Exception("replication not completed");
                    }

                    var res = await c8oTask.CallJson("fs://.post",
                        "_use_policy", FullSyncPolicy.MERGE.value,
                        "_id", task["_id"].Value<string>(),
                        "replicated", task["replicated"] = true
                    ).Async();
                    Debug("replicated true:\n" + res);
                }

                if (!task["assembled"].Value<bool>())
                {
                    downloadStatus.State = DownloadStatus.StateAssembling;
                    Notify(downloadStatus);
                    //
                    // 2 : Gets the document describing the chunks list
                    //
                    var createdFileStream = fileManager.CreateFile(downloadStatus.Filepath);
                    createdFileStream.Position = 0;

                    for (int i = 0; i < downloadStatus.Total; i++)
                    {
                        var meta = await c8o.CallJson("fs://.get", "docid", downloadStatus.Uuid + "_" + i).Async();
                        Debug(meta.ToString());

                        AppendChunk(createdFileStream, meta.SelectToken("_attachments.chunk.content_url").ToString());
                    }
                    createdFileStream.Dispose();

                    var res = await c8o.CallJson("fs://.destroy").Async();
                    Debug("destroy local true:\n" + res.ToString());

                    res = await c8oTask.CallJson("fs://.post",
                        "_use_policy", FullSyncPolicy.MERGE.value,
                        "_id", task["_id"].Value<string>(),
                        "assembled", task["assembled"] = true
                    ).Async();
                    Debug("assembled true:\n" + res);
                }

                if (!task["remoteDeleted"].Value<bool>() && authenticated)
                {
                    downloadStatus.State = DownloadStatus.StateCleaning;
                    Notify(downloadStatus);

                    var res = await c8o.CallJson(".DeleteUuid", "uuid", downloadStatus.Uuid).Async();
                    Debug("deleteUuid:\n" + res);

                    res = await c8oTask.CallJson("fs://.post",
                        "_use_policy", FullSyncPolicy.MERGE.value,
                        "_id", task["_id"].Value<string>(),
                        "remoteDeleted", task["remoteDeleted"] = true
                    ).Async();
                    Debug("remoteDeleted true:\n" + res);
                }

                if (task["replicated"].Value<bool>() && task["assembled"].Value<bool>() && task["remoteDeleted"].Value<bool>())
                {
                    var res = await c8oTask.CallJson("fs://.delete", "docid", downloadStatus.Uuid).Async();
                    Debug("local delete:\n" + res.ToString());

                    downloadStatus.State = DownloadStatus.StateFinished;
                    Notify(downloadStatus);
                }
            }
            catch (Exception e)
            {
                Notify(e);
            }

            tasks.Remove(downloadStatus.Uuid);
            
            lock (this)
            {
                Monitor.Pulse(this);
            }
        }

        private void AppendChunk(Stream createdFileStream, string contentPath)
        {
            Stream chunkStream;
            if (contentPath.StartsWith("http://") || contentPath.StartsWith("https://"))
            {
                var request = HttpWebRequest.CreateHttp(contentPath);
                var response = Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request).Result as HttpWebResponse;
                chunkStream = response.GetResponseStream();
            }
            else
            {
                string contentPath2 = UrlToPath(contentPath);
                chunkStream = fileManager.OpenFile(contentPath2);
            }
            chunkStream.CopyTo(createdFileStream, 4096);
            chunkStream.Dispose();
            createdFileStream.Position = createdFileStream.Length;
        }

        private static string UrlToPath(string url)
        {
            // Checks if the URL is valid
            string fileProtocol = "file://";
            if (url.Length > fileProtocol.Length && url.StartsWith(fileProtocol))
            {
                // Finds the file path
                url = url.Substring(fileProtocol.Length);
            }
            // URL decode the string
            url = Uri.UnescapeDataString(url);
            return url;
        }

        private void Notify(DownloadStatus downloadStatus)
        {
            if (RaiseDownloadStatus != null)
            {
                RaiseDownloadStatus(this, downloadStatus);
            }
        }

        private void Notify(Exception exception)
        {
            if (RaiseException != null)
            {
                RaiseException(this, exception);
            }
        }

        private void Debug(string debug)
        {
            if (RaiseDebug != null)
            {
                RaiseDebug(this, debug);
            }
        }
    }
}
