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
    /// <summary>
    /// This class manages big file transfers from and to Convertigo Server. To transfer a file you need to install in the 
    /// Convertigo server the lib_FileTransfer.car project handling the backend part. 
    /// 
    /// File transfers are using FullSync technology to transfer files in chunk mode. When a transfer is requested, the server
    /// will cut the file in chunks, then will insert the chunks in a FullSync database, the Database will replicate and the file will be reassembled
    /// on the client side.
    /// </summary>
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

        /// <summary>
        /// Initialize a File transfer. This will prepare everything needed to transfer a file. The name of the backend project and
        /// the name of the FullSync status database will be set by default to <b>lib_FileTransfer</b> and to <b>c8ofiletransfer_tasks</b> but
        /// you can override them passing custom values the <b>projectname</b> and the <b>taskDb</b> parameters.
        /// </summary>
        /// <param name="c8o">An initilized C8o endpoint object</param>
        /// <param name="projectName">the overrided project name</param>
        /// <param name="taskDb">the overrided status database name</param>
        /// <sample>
        ///     Typical usage : 
        ///     <code>
        ///         // Construct the endpoint to Convertigo Server
        ///         c8o = new C8o("http://[server:port]/convertigo/projects/[my__backend_project]");
        /// 
        ///         // Buid a C8oFileTransfer object
        ///         fileTransfer = new C8oFileTransfer(c8o);
        /// 
        ///         // Attach a TransferStatus monitor
        ///         fileTransfer.RaiseTransferStatus += (sender, transferStatus) => {
        ///             // Do Whatever has to be done to monitor the transfer
        ///         };
        /// 
        ///         // Start Transfer engine
        ///         fileTransfer.Start();
        /// 
        ///         // DO Some Stuff
        ///         ....
        ///         // Call a custom Sequence in the server responsible for getting the document to be transffered from any
        ///         // Repository and pushing it to FullSync using the lib_FileTransfer.var library.
        ///         JObject data = await c8o.CallJSON(".AddFileXfer").Async();
        /// 
        ///         // This sequence should return an uuid identifying the transfer.
        ///         String uuid = ["document"]["uuid"].Value();
        /// 
        ///         // Use this uuid to start the transfer and give the target filename and path on your device file system.
        ///         fileTransfer.DownloadFile(uuid, "c:\\temp\\MyTransferredFile.data");
        ///     </code>
        /// </sample>
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

        /// <summary>
        /// Add a file to transfer to the download queue. This call must be done after getting a uuid from the Convertigo Server. 
        /// the uuid is generated by the server by calling the RequestFile file Sequence.
        /// </summary>
        /// <param name="uuid">a uuid obtained by a call to the 'RequestFile' sequence on the server</param>
        /// <param name="filepath">a path where the file will be assembled when the transfer is finished</param>
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
