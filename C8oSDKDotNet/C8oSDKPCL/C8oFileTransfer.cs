using Convertigo.SDK.FullSync.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public class C8oFileTransfer
    {
        static internal C8oFileManager fileManager;

        private bool tasksDbCreated = false;
        private bool alive = true;
        
        private C8o c8oTask;
        private Dictionary<string, C8oFileTransferStatus> tasks = null;
        public event EventHandler<C8oFileTransferStatus> RaiseTransferStatus;
        public event EventHandler<string> RaiseDebug;
        public event EventHandler<Exception> RaiseException;
        
        public C8oFileTransfer(C8o c8o, string projectName = "lib_FileTransfer", string taskDb = "c8ofiletransfer_tasks")
        {
            c8oTask = new C8o(c8o.EndpointConvertigo + "/projects/" + projectName, new C8oSettings(c8o).SetDefaultDatabaseName(taskDb));
        }

        public void Start()
        {
            if (tasks == null)
            {
                tasks = new Dictionary<string, C8oFileTransferStatus>();
                
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

                                    var transferStatus = tasks[uuid] = new C8oFileTransferStatus(uuid, filePath);
                                    Notify(transferStatus);

                                    DownloadFile(transferStatus, task).GetAwaiter();

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

        public async Task DownloadFile(string uuid, string filePath)
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

        async Task DownloadFile(C8oFileTransferStatus transferStatus, JObject task)
        {
            try
            {
                var c8o = new C8o(c8oTask.Endpoint, new C8oSettings(c8oTask).SetFullSyncLocalSuffix("_" + transferStatus.Uuid));
                string fsConnector = null;

                //
                // 0 : Authenticates the user on the Convertigo server in order to replicate wanted documents
                //
                if (!task["replicated"].Value<bool>() || !task["remoteDeleted"].Value<bool>())
                {
                    var json = await c8o.CallJson(".SelectUuid", "uuid", transferStatus.Uuid).Async();

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
                        fsConnector = json.SelectToken("document.connector").ToString();
                        transferStatus.State = C8oFileTransferStatus.StateAuthenticated;
                        Notify(transferStatus);
                    }
                }

                //
                // 1 : Replicate the document discribing the chunks ids list
                //

                if (!task["replicated"].Value<bool>() && fsConnector != null)
                {
                    var locker = new bool[] { false };

                    await c8o.CallJson("fs://" + fsConnector + ".create").Async();

                    c8o.CallJson("fs://" + fsConnector + ".replicate_pull").Then((json, param) =>
                    {
                        lock (locker)
                        {
                                locker[0] = true;
                                Monitor.Pulse(locker);
                        }
                        return null;
                    });

                    transferStatus.State = C8oFileTransferStatus.StateReplicate;
                    Notify(transferStatus);

                    var allOptions = new Dictionary<string, object> {
                        { "startkey", '"' + transferStatus.Uuid + "_\"" },
                        { "endkey", '"' + transferStatus.Uuid + "__\"" }
                    };

                    // Waits the end of the replication if it is not finished
                    while (!locker[0])
                    {
                        try
                        {
                            lock (locker)
                            {
                                Monitor.Wait(locker, 500);
                            }

                            var all = await c8o.CallJson("fs://" + fsConnector + ".all", allOptions).Async();
                            var rows = all["rows"];
                            if (rows != null)
                            {
                                int current = (rows as JArray).Count;
                                if (current != transferStatus.Current)
                                {
                                    transferStatus.Current = current;
                                    Notify(transferStatus);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug(e.ToString());
                        }
                    }

                    if (transferStatus.Current < transferStatus.Total)
                    {
                        throw new Exception("replication not completed");
                    }

                    var res = await c8oTask.CallJson("fs://" + fsConnector + ".post",
                        "_use_policy", FullSyncPolicy.MERGE.value,
                        "_id", task["_id"].Value<string>(),
                        "replicated", task["replicated"] = true
                    ).Async();
                    Debug("replicated true:\n" + res);
                }

                if (!task["assembled"].Value<bool>() && fsConnector != null)
                {
                    transferStatus.State = C8oFileTransferStatus.StateAssembling;
                    Notify(transferStatus);
                    //
                    // 2 : Gets the document describing the chunks list
                    //
                    var createdFileStream = fileManager.CreateFile(transferStatus.Filepath);
                    createdFileStream.Position = 0;

                    for (int i = 0; i < transferStatus.Total; i++)
                    {
                        var meta = await c8o.CallJson("fs://" + fsConnector + ".get", "docid", transferStatus.Uuid + "_" + i).Async();
                        Debug(meta.ToString());

                        AppendChunk(createdFileStream, meta.SelectToken("_attachments.chunk.content_url").ToString());
                    }
                    createdFileStream.Dispose();

                    var res = await c8oTask.CallJson("fs://.post",
                        "_use_policy", FullSyncPolicy.MERGE.value,
                        "_id", task["_id"].Value<string>(),
                        "assembled", task["assembled"] = true
                    ).Async();
                    Debug("assembled true:\n" + res);
                }

                if (!task["remoteDeleted"].Value<bool>() && fsConnector != null)
                {
                    transferStatus.State = C8oFileTransferStatus.StateCleaning;
                    Notify(transferStatus);
                    
                    var res = await c8o.CallJson("fs://" + fsConnector + ".destroy").Async();
                    Debug("destroy local true:\n" + res.ToString());

                    res = await c8o.CallJson(".DeleteUuid", "uuid", transferStatus.Uuid).Async();
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
                    var res = await c8oTask.CallJson("fs://.delete", "docid", transferStatus.Uuid).Async();
                    Debug("local delete:\n" + res.ToString());

                    transferStatus.State = C8oFileTransferStatus.StateFinished;
                    Notify(transferStatus);
                }
            }
            catch (Exception e)
            {
                Notify(e);
            }

            tasks.Remove(transferStatus.Uuid);
            
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
            // Lesson learnt - always check for a valid URI
            try
            {
                Uri uri = new Uri(url);
                url = uri.LocalPath;
            }
            catch
            {
                // not uri format
            }
            // URL decode the string
            url = Uri.UnescapeDataString(url);
            return url;
        }

        private void Notify(C8oFileTransferStatus transferStatus)
        {
            if (RaiseTransferStatus != null)
            {
                RaiseTransferStatus(this, transferStatus);
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
