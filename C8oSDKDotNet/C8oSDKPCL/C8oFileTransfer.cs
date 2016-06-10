using Convertigo.SDK.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        private int chunkSize = 1000 * 1024;

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

                                // If this document id is not already in the tasks list
                                if (!tasks.ContainsKey(uuid) && (task["download"] != null || task["upload"] != null))
                                {

                                    // await c8oTask.CallJson("fs://.delete", "docid", uuid).Async();

                                    string filePath = task["filePath"].Value<string>();

                                    // Add the document id to the tasks list
                                    var transferStatus = tasks[uuid] = new C8oFileTransferStatus(uuid, filePath);
                                    Notify(transferStatus);

                                    if (task["download"] != null)
                                    {
                                        DownloadFile(transferStatus, task).GetAwaiter();
                                    }
                                    else if (task["upload"] != null)
                                    {
                                        UploadFile(transferStatus, task).GetAwaiter();
                                    }

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
                "remoteDeleted", false,
                "download", 0
            ).Async();

            lock (this)
            {
                Monitor.Pulse(this);
            }
        }


        async Task DownloadFile(C8oFileTransferStatus transferStatus, JObject task)
        {
            bool needRemoveSession = false;
            C8o c8o = null;
            try
            {
                c8o = new C8o(c8oTask.Endpoint, new C8oSettings(c8oTask).SetFullSyncLocalSuffix("_" + transferStatus.Uuid));
                string fsConnector = null;

                //
                // 0 : Authenticates the user on the Convertigo server in order to replicate wanted documents
                //
                if (!task["replicated"].Value<bool>() || !task["remoteDeleted"].Value<bool>())
                {
                    needRemoveSession = true;
                    var json = await c8o.CallJson(".SelectUuid", "uuid", transferStatus.Uuid).Async();

                    Debug("SelectUuid:\n" + json.ToString());

                    if (!"true".Equals(json.SelectToken("document.selected").Value<string>()))
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

                    needRemoveSession = true;
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
                        { "startkey", transferStatus.Uuid + "_" },
                        { "endkey", transferStatus.Uuid + "__" }
                    };

                    // Waits the end of the replication if it is not finished
                    do
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
                    while (!locker[0]);

                    if (transferStatus.Current < transferStatus.Total)
                    {
                        throw new Exception("replication not completed");
                    }

                    var res = await c8oTask.CallJson("fs://" + fsConnector + ".post",
                        C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
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
                        C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
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

                    needRemoveSession = true;
                    res = await c8o.CallJson(".DeleteUuid", "uuid", transferStatus.Uuid).Async();
                    Debug("deleteUuid:\n" + res);

                    res = await c8oTask.CallJson("fs://.post",
                        C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
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

            if (needRemoveSession && c8o != null)
            {
                c8o.CallJson(".RemoveSession");
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

        /// <summary>
        /// Called by the UI to add an upload file request to the queue.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task UploadFile(String filePath)
        {
            // Creates the database if it doesn't exist
            await CheckTaskDb();

            // Initializes the uuid ending with the number of chunks
            string uuid = System.Guid.NewGuid().ToString();
            Stream fileStream = fileManager.OpenFile(filePath);
            long fileSize = fileStream.Length;
            fileStream.Dispose();
            double d = (double)fileSize / chunkSize;
            double numberOfChunks = Math.Ceiling(d);
            uuid = uuid + "-" + numberOfChunks;

            // Posts a document describing the state of the upload in the task db
            await c8oTask.CallJson("fs://.post",
                 "_id", uuid,
                 "filePath", filePath,
                 "splitted", false,
                 "replicated", false,
                 "localDeleted", false,
                 "assemblingRequest", false,
                 "upload", 0
             ).Async();

            lock (this)
            {
                Monitor.Pulse(this);
            }
        }

        /// <summary>
        /// Called 
        /// </summary>
        /// <returns></returns>
        async Task UploadFile(C8oFileTransferStatus transferStatus, JObject task)
        {

            C8o c8o = null;

            try
            {

                // await c8oTask.CallJson("fs://.delete", "docid", transferStatus.Uuid).Async();
                // return;

                JObject tmp = null;
                JObject res = null;
                bool[] locker = null;

                // Creates a c8o instance with a specific fullsync local suffix in order to store chunks in a specific database
                c8o = new C8o(c8oTask.Endpoint, new C8oSettings(c8oTask).SetFullSyncLocalSuffix("_" + transferStatus.Uuid).SetDefaultDatabaseName("c8ofiletransfer"));
                c8o.LogLevelLocal = C8oLogLevel.WARN;

                tmp = await c8o.CallJson("fs://.create").Async();
                // tmp = await c8o.CallJson("fs://.all").Async();

                // If the file is not already splitted and stored in the local database
                if (!task["splitted"].Value<bool>())
                {
                    // Retrieves the file
                    string filePath = transferStatus.Filepath;
                    string fileName = Path.GetFileName(filePath);
                    Stream fileStream = fileManager.OpenFile(filePath);
                    MemoryStream chunk = new MemoryStream(chunkSize);
                    fileStream.Position = 0;

                    //
                    // 1 : Split the file and store it locally
                    //
                    try
                    {               
                        string uuid = transferStatus.Uuid;

                       // DateTime Start = DateTime.Now;

                        for (int chunkId = 0; chunkId < transferStatus.Total; chunkId++)
                        {
                            string docid = uuid + "_" + chunkId;

                            // Checks if the chunk is not already stored to avoid conflicts
                            bool documentAlreadyExists = false;
                            bool chunkAlreadyExists = false;
                            try
                            {
                                tmp = await c8o.CallJson("fs://.get",
                                    "docid", docid
                                ).Async();
                                documentAlreadyExists = true;
                            }
                            catch (Exception e)
                            {
                                documentAlreadyExists = false;
                            }

                            if (!documentAlreadyExists)
                            {
                               tmp = await c8o.CallJson("fs://.post",
                                    "_id", docid,
                                    "fileName", fileName,
                                    "type", "chunk",
                                    "uuid", uuid
                                ).Async();
                            }

                            if (!chunkAlreadyExists)
                            {
                                byte[] buffer = new byte[chunkSize];
                                // fileStream.Position = chunkSize * chunkId;                                
                                int read = fileStream.Read(buffer, 0, chunkSize);

                                chunk = new MemoryStream(chunkSize);
                                chunk.Position = 0;
                                chunk.Write(buffer, 0, read);

                                tmp = await c8o.CallJson("fs://.put_attachment",
                                    "docid", docid,
                                    "name", "chunk",
                                    "content_type", "application/octet-stream",
                                    "content", chunk).Async();

                                // await chunk.FlushAsync();
                                chunk.Dispose();
                            }
                        }

                        /*DateTime Stop = DateTime.Now;
                        TimeSpan elapsed = Stop - Start;
                        c8o.c8oLogger.Error("Time elapsed : " + elapsed.ToString());*/

                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    finally
                    {
                        if (fileStream != null)
                        {
                            fileStream.Dispose();
                        }
                        if (chunk != null)
                        {
                            chunk.Dispose();
                        }
                    }


                    // Updates the state document in the c8oTask database
                    res = await c8oTask.CallJson("fs://.post",
                        C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                        "_id", task["_id"].Value<string>(),
                        "splitted", task["splitted"] = true
                    ).Async();
                    Debug("splitted true:\n" + res);
                }

                // tmp = await c8o.CallJson("fs://.all").Async();

                // If the local database is not replecated to the server
                if (!task["replicated"].Value<bool>())
                {
                    //
                    // 2 : Authenticates
                    //
                    tmp = await c8o.CallJson(".SetAuthenticatedUser", "userId", transferStatus.Uuid).Async();

                    //
                    // 3 : Replicates to server
                    //
                    bool launchreplication = true;

                    // Relaunch replication while all docupments are not replicated to the server
                    while (launchreplication)
                    {
                        locker = new bool[] { false };
                        c8o.CallJson("fs://.replicate_push").Progress((c8oOnProgress) =>
                        {
                            if (c8oOnProgress.Finished)
                            {
                                if (c8oOnProgress.Total == 0)
                                {
                                    launchreplication = false;
                                }

                                lock (locker)
                                {
                                    locker[0] = true;
                                    Monitor.Pulse(locker);
                                }
                            }    
                        });

                        // Waits the end of the replication if it is not finished
                        do
                        {
                            lock (locker)
                            {
                                Monitor.Wait(locker, 500);
                            }
                        } while (!locker[0]);
                    }

                    // tmp = await c8o.CallJson("lib_FileTransfer.c8ofiletransfer.AllChunks", "uuid", transferStatus.Uuid).Async();

                    // Updates the state document in the c8oTask database
                    res = await c8oTask.CallJson("fs://.post",
                        C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                        "_id", task["_id"].Value<string>(),
                        "replicated", task["replicated"] = true
                    ).Async();
                    Debug("replicated true:\n" + res);
                }

                // return;

                // If the local database containing chunks is not deleted
                locker = new bool[] { true };
                if (!task["localDeleted"].Value<bool>())
                {
                    locker[0] = false;
                    //
                    // 4 : Delete the local database containing chunks
                    //
                    c8o.CallJson("fs://.reset").Then((json, param) =>
                    {
                        c8oTask.CallJson("fs://.post",
                            C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                            "_id", task["_id"].Value<string>(),
                            "localDeleted", task["localDeleted"] = true
                        );
                        Debug("localDeleted true:\n" + res);

                        lock (locker)
                        {
                            locker[0] = true;
                            Monitor.Pulse(locker);
                        }

                        return null;
                    });
                }

                // If the file is not assembled in the server
                if (!task["assemblingRequest"].Value<bool>())
                {
                    //
                    // 5 : Request the server to assemble chunks to the initial file
                    //
                    tmp = await c8o.CallJson(".StoreDatabaseFileToLocal", "uuid", transferStatus.Uuid).Async();
                    c8oTask.CallJson("fs://.post",
                            C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                            "_id", task["_id"].Value<string>(),
                            "assemblingRequest", task["assemblingRequest"] = true
                        );
                    Debug("assemblingRequest true:\n" + res);
                }

                // Waits the local database is deleted
                do
                {
                    lock (locker)
                    {
                        Monitor.Wait(locker, 500);
                    }
                } while (!locker[0]);

                //
                // 6 : Remove the task document
                //
                res = await c8oTask.CallJson("fs://.delete", "docid", transferStatus.Uuid).Async();
                Debug("task document delete:\n" + res.ToString());
            }
            catch (Exception e)
            {
                Notify(e);
            }
        }
    }
}
