using Convertigo.SDK;
using Convertigo.SDK;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Convertigo.SDK.Utils;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Convertigo.SDK.Exceptions;
using System.Net;
using Convertigo.SDK.FullSync.Enums;

namespace C8oBigFileTransfer
{
    public class BigFileTransferInterface
    {
        private String endpoint;
        private C8oSettings c8oSettings;
        private FileManager fileManager;
        private C8o c8oTask;
        private bool tasksDbCreated = false;
        private bool alive = true;
        private Dictionary<String, DownloadStatus> tasks = null;
        public event EventHandler<DownloadStatus> RaiseDownloadStatus;
        public event EventHandler<String> RaiseDebug;
        public event EventHandler<Exception> RaiseException;
        
        public BigFileTransferInterface(String endpoint, C8oSettings c8oSettings, FileManager fileManager, String taskDb = "bigfiletransfer_tasks")
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
                tasks = new Dictionary<String, DownloadStatus>();
                
                Task.Factory.StartNew(async () =>
                {
                    await CheckTaskDb();
                    int skip = 0;

                    Dictionary<String, Object> param = new Dictionary<String, Object>
                    {
                        {"limit", 1},
                        {"include_docs", true}
                    };

                    while (alive)
                    {
                        try
                        {
                            param["skip"] = skip;
                            JObject res = await c8oTask.CallJson("fs://.all", param).Async();

                            if ((res["rows"] as JArray).Count > 0)
                            {
                                JObject task = res["rows"][0]["doc"] as JObject;
                                if (task == null)
                                {
                                    task = await c8oTask.CallJson("fs://.get", new Dictionary<String, Object>{{"docid", res["rows"][0]["id"].ToString()}}).Async();
                                }
                                String uuid = task["_id"].ToString();

                                if (!tasks.ContainsKey(uuid))
                                {
                                    String filePath = task["filePath"].Value<String>();

                                    DownloadStatus downloadStatus = tasks[uuid] = new DownloadStatus(uuid, filePath);
                                    Notify(downloadStatus);

                                    await Task.Factory.StartNew(async () =>
                                    {
                                        await DownloadFile(downloadStatus, task);

                                    });
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
                });
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

        public async Task AddFile(String uuid, String filePath)
        {
            await CheckTaskDb();

            JObject res = await c8oTask.CallJson("fs://.post", new Dictionary<String, Object>
            {
                {"_id", uuid},
                {"filePath", filePath},
                {"replicated", false},
                {"assembled", false},
                {"remoteDeleted", false}
            }).Async();

            lock (this)
            {
                Monitor.Pulse(this);
            }
        }

        public async Task DownloadFile(DownloadStatus downloadStatus, JObject task)
        {
            try
            {
                C8o c8o = new C8o(endpoint, c8oSettings.Clone().SetFullSyncLocalSuffix("_" + downloadStatus.Uuid));
                bool authenticated = false;

                //
                // 0 : Authenticates the user on the Convertigo server in order to replicate wanted documents
                //
                if (!task["replicated"].Value<bool>() || !task["remoteDeleted"].Value<bool>())
                {
                    JObject json = await c8o.CallJson(".SelectUuid", new Dictionary<String, Object> { { "uuid", downloadStatus.Uuid } }).Async();

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
                    Boolean[] locker = new Boolean[] { false };

                    await c8o.CallJson("fs://.create").Async();

                    c8o.Call("fs://.replicate_pull", null, new C8oResponseJsonListener((jsonResponse, requestParameters) =>
                    {
                        Debug("Replicate:\n" + jsonResponse.ToString());

                        String status;
                        if (C8oUtils.TryGetValueAndCheckType<String>(jsonResponse, "status", out status))
                        {
                            // Checks the replication status
                            lock (locker)
                            {
                                if (status.Equals("Active"))
                                {
                                    // locker[0] = true;
                                }
                                else if (status.Equals("Offline"))
                                {
                                    // locker[0] = false;
                                    Monitor.Pulse(locker);
                                }
                                else if (status.Equals("Stopped"))
                                {
                                    locker[0] = true;
                                    Monitor.Pulse(locker);
                                }
                            }
                        }
                    }));

                    downloadStatus.State = DownloadStatus.StateReplicate;
                    Notify(downloadStatus);

                    Dictionary<String, Object> allOptions = new Dictionary<String, Object> {
                        { "startkey", '"' + downloadStatus.Uuid + "_\"" },
                        { "endkey", '"' + downloadStatus.Uuid + "__\"" }
                    };

                    // Waits the end of the replication if it is not finished
                    while (!locker[0])
                    {
                        
                        try
                        {
                            JObject all = await c8o.CallJson("fs://.all", allOptions).Async();
                            JToken rows = all["rows"];
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

                    JObject res = await c8oTask.CallJson("fs://.post", new Dictionary<String, Object> {
                        {"_use_policy", FullSyncPolicy.MERGE.value},
                        {"_id", task["_id"].Value<String>()},
                        {"replicated", task["replicated"] = true}
                    }).Async();
                    Debug("replicated true:\n" + res.ToString());
                }

                if (!task["assembled"].Value<bool>())
                {
                    downloadStatus.State = DownloadStatus.StateAssembling;
                    Notify(downloadStatus);
                    //
                    // 2 : Gets the document describing the chunks list
                    //
                    Stream createdFileStream = fileManager.CreateFile(downloadStatus.Filepath);
                    createdFileStream.Position = 0;

                    for (int i = 0; i < downloadStatus.Total; i++)
                    {
                        JObject meta = await c8o.CallJson("fs://.get", new Dictionary<String, Object> { { "docid", downloadStatus.Uuid + "_" + i } }).Async();
                        Debug(meta.ToString());

                        AppendChunk(createdFileStream, meta.SelectToken("_attachments.chunk.content_url").ToString());
                    }
                    createdFileStream.Dispose();

                    JObject res = await c8o.CallJson("fs://.destroy").Async();
                    Debug("destroy local true:\n" + res.ToString());

                    res = await c8oTask.CallJson("fs://.post", new Dictionary<String, Object> {
                        {"_use_policy", FullSyncPolicy.MERGE.value},
                        {"_id", task["_id"].Value<String>()},
                        {"assembled", task["assembled"] = true}
                    }).Async();
                    Debug("assembled true:\n" + res.ToString());
                }

                if (!task["remoteDeleted"].Value<bool>() && authenticated)
                {
                    downloadStatus.State = DownloadStatus.StateCleaning;
                    Notify(downloadStatus);

                    JObject res = await c8o.CallJson(".DeleteUuid", new Dictionary<String, Object> { { "uuid", downloadStatus.Uuid } }).Async();
                    Debug("deleteUuid:\n" + res.ToString());

                    res = await c8oTask.CallJson("fs://.post", new Dictionary<String, Object> {
                        {"_use_policy", FullSyncPolicy.MERGE.value},
                        {"_id", task["_id"].Value<String>()},
                        {"remoteDeleted", task["remoteDeleted"] = true}
                    }).Async();
                    Debug("remoteDeleted true:\n" + res.ToString());
                }

                if (task["replicated"].Value<bool>() && task["assembled"].Value<bool>() && task["remoteDeleted"].Value<bool>())
                {
                    JObject res = await c8oTask.CallJson("fs://.delete", new Dictionary<String, Object> { { "docid", downloadStatus.Uuid } }).Async();
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

        private void AppendChunk(Stream createdFileStream, String contentPath)
        {
            Stream chunkStream;
            if (contentPath.StartsWith("http://") || contentPath.StartsWith("https://"))
            {
                HttpWebRequest request = HttpWebRequest.CreateHttp(contentPath);
                HttpWebResponse response = Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request).Result as HttpWebResponse;
                chunkStream = response.GetResponseStream();
            }
            else
            {
                String contentPath2 = UrlToPath(contentPath);
                chunkStream = fileManager.OpenFile(contentPath2);
            }
            chunkStream.CopyTo(createdFileStream, 4096);
            chunkStream.Dispose();
            createdFileStream.Position = createdFileStream.Length;
        }

        private static String UrlToPath(String url)
        {
            // Checks if the URL is valid
            String fileProtocol = "file://";
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

        private void Debug(String debug)
        {
            if (RaiseDebug != null)
            {
                RaiseDebug(this, debug);
            }
        }
    }
}
