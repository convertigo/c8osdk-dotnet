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
    public class C8oFileTransfer : C8oFileTransferBase
    {
        static internal C8oFileManager fileManager;

        private bool tasksDbCreated = false;
        private bool alive = true;

        private int chunkSize = 1000 * 1024;
        private int[] _maxRunning;

        private C8o c8oTask;
        private Dictionary<string, C8oFileTransferStatus> tasks = null;
        private ISet<string> canceledTasks = new HashSet<string>();

        /// <summary>
        /// Register an event handler about transfer status update.
        /// Each step of the transfer will notify this handler.
        /// </summary>
        public event EventHandler<C8oFileTransferStatus> RaiseTransferStatus;
        /// <summary>
        /// Register an event handler about debug information of the filetransfer.
        /// Each internal debug will notify this handler.
        /// </summary>
        public event EventHandler<string> RaiseDebug;
        /// <summary>
        /// Register an event handler about exception that can append during a transfer.
        /// Each internal exception will notify this handler.
        /// </summary>
        public event EventHandler<Exception> RaiseException;

        private Dictionary<string, Stream> streamToUpload;

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
        public C8oFileTransfer(C8o c8o, C8oFileTransferSettings c8oFileTransferSettings = null)
        {
            if (c8oFileTransferSettings != null)
            {
                Copy(c8oFileTransferSettings);
            }
            _maxRunning = new int[] { maxRunning };
            c8oTask = new C8o(c8o.EndpointConvertigo + "/projects/" + projectName, new C8oSettings(c8o).SetDefaultDatabaseName(taskDb));
            streamToUpload = new Dictionary<string, Stream>();
        }

        /// <summary>
        /// Start the filetransfer loop, should be called after "Raise" handler configuration.
        /// </summary>
        public void Start()
        {
            if (tasks == null)
            {
                tasks = new Dictionary<string, C8oFileTransferStatus>();

                Task.Factory.StartNew(async () =>
                {
                    CheckTaskDb();
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

                                    //await c8oTask.CallJson("fs://.delete", "docid", uuid).Async();
                                    //continue;

                                    string filePath = FilePrefix + task["filePath"].Value<string>();

                                    // Add the document id to the tasks list
                                    var transferStatus = tasks[uuid] = new C8oFileTransferStatus(uuid, filePath);
                                    transferStatus.State = C8oFileTransferStatus.StateQueued;

                                    if (task["download"] != null)
                                    {
                                        transferStatus.Current = task["download"].Value<int>();
                                        transferStatus.IsDownload = true;
                                        DownloadFile(transferStatus, task).GetAwaiter();
                                    }
                                    else if (task["upload"] != null)
                                    {
                                        transferStatus.IsDownload = false;
                                        UploadFile(transferStatus, task).GetAwaiter();
                                    }

                                    Notify(transferStatus);

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

        private void CheckTaskDb()
        {
            lock (c8oTask)
            {
                if (!tasksDbCreated)
                {
                    Debug("Creation of the c8oTask DB.");
                    c8oTask.CallJson("fs://.create").Sync();
                    tasksDbCreated = true;
                }
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
            CheckTaskDb();

            try
            {
                await c8oTask.CallJson("fs://.post",
                    "_id", uuid,
                    "filePath", filePath,
                    "replicated", false,
                    "assembled", false,
                    "remoteDeleted", false,
                    "download", 0
                ).Async();
            }
            catch (Exception e)
            {
                throw new C8oException("Skip DownloadFile request with UUID " + uuid + ": already added.", e);
            }

            lock (this)
            {
                Monitor.Pulse(this);
            }
        }


        async Task DownloadFile(C8oFileTransferStatus transferStatus, JObject task)
        {
            var uuid = transferStatus.Uuid;
            bool needRemoveSession = false;

            C8o c8o = null;
            Stream createdFileStream = null;
            try
            {
                lock (_maxRunning)
                {
                    if (_maxRunning[0] <= 0)
                    {
                        Monitor.Wait(_maxRunning);
                    }
                    _maxRunning[0]--;
                }

                c8o = new C8o(c8oTask.Endpoint, new C8oSettings(c8oTask).SetFullSyncLocalSuffix("_" + uuid));
                string fsConnector = null;

                //
                // 0 : Authenticates the user on the Convertigo server in order to replicate wanted documents
                //
                if (!task["replicated"].Value<bool>() || !task["remoteDeleted"].Value<bool>() || !task["assembled"].Value<bool>())
                {
                    needRemoveSession = true;
                    var json = await c8o.CallJson(".SelectUuid", "uuid", uuid).Async();

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

                if (!useCouchBaseReplication && !task["replicated"].Value<bool>() && fsConnector != null && !canceledTasks.Contains(uuid))
                {
                    var filepath = transferStatus.Filepath + ".tmp";
                    createdFileStream = fileManager.CreateFile(filepath);

                    var res = await c8oTask.CallJson("fs://.get", "docid", task["_id"].Value<string>()).Async();

                    var pos = res["position"] != null ? res["position"].Value<long>() : 0;
                    var last = res["download"].Value<int>();
                    createdFileStream.Position = pos;
                    transferStatus.Current = last;

                    transferStatus.State = C8oFileTransferStatus.StateReplicate;
                    Notify(transferStatus);

                    for (var i = last; i < transferStatus.Total; i++)
                    {
                        await DownloadChunk(c8o, createdFileStream, filepath, fsConnector, uuid, i, task, transferStatus);
                    }
                    createdFileStream.Dispose();
                    res = await c8oTask.CallJson("fs://.post",
                        C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                        "_id", task["_id"].Value<string>(),
                        "replicated", task["replicated"] = true
                    ).Async();
                    Debug("replicated true:\n" + res);
                }

                //
                // 1 : Replicate the document discribing the chunks ids list
                //
                if (useCouchBaseReplication && !task["replicated"].Value<bool>() && fsConnector != null && !canceledTasks.Contains(uuid))
                {
                    var locker = new bool[] { false };
                    var expireTransfer = DateTime.Now.Add(MaxDurationForTransferAttempt);
                    var expireChunk = expireTransfer;

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
                    }).Progress(progress =>
                    {
                        if (progress.Status.Equals("Active"))
                        {
                            expireChunk = DateTime.Now.Add(MaxDurationForChunk);
                            int current = (int) Math.Max(0, progress.Current - 2);
                            if (current > transferStatus.Current)
                            {
                                transferStatus.Current = current;
                                Notify(transferStatus);
                            }
                        }
                        else
                        {
                            Debug("======== NOT ACTIVE >> progress:\n" + progress);
                            expireChunk = expireTransfer;
                        }
                    });

                    transferStatus.State = C8oFileTransferStatus.StateReplicate;
                    Notify(transferStatus);

                    var allOptions = new Dictionary<string, object> {
                        { "startkey", uuid + "_" },
                        { "endkey", uuid + "__" }
                    };

                    // Waits the end of the replication if it is not finished
                    do
                    {
                        try
                        {
                            lock (locker)
                            {
                                if (DateTime.Now > expireTransfer)
                                {
                                    locker[0] = true;
                                    throw new Exception("expireTransfer of " + MaxDurationForTransferAttempt + " : retry soon");
                                }
                                if (DateTime.Now > expireChunk)
                                {
                                    locker[0] = true;
                                    throw new Exception("expireChunk of " + MaxDurationForTransferAttempt + " : retry soon");
                                }
                                Monitor.Wait(locker, 1000);
                            }
                        }
                        catch (Exception e)
                        {
                            Notify(e);
                            Debug(e.ToString());
                        }
                    }
                    while (!locker[0] && !canceledTasks.Contains(uuid));

                    c8o.CallJson("fs://" + fsConnector + ".replicate_pull", "cancel", true).Sync();

                    if (!canceledTasks.Contains(uuid))
                    {
                        if (transferStatus.Current < transferStatus.Total)
                        {
                            throw new Exception("replication not completed");
                        }

                        var res = await c8oTask.CallJson("fs://.post",
                            C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                            "_id", task["_id"].Value<string>(),
                            "replicated", task["replicated"] = true
                        ).Async();
                        Debug("replicated true:\n" + res);
                    }
                }

                var isCanceling = canceledTasks.Contains(uuid);

                if (!useCouchBaseReplication && !task["assembled"].Value<bool>() && fsConnector != null && !isCanceling)
                {
                    transferStatus.State = C8oFileTransferStatus.StateAssembling;
                    Notify(transferStatus);

                    var filepath = transferStatus.Filepath;
                    fileManager.DeleteFile(filepath);
                    fileManager.MoveFile(filepath + ".tmp", filepath);

                    var res = await c8oTask.CallJson("fs://.post",
                        C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                        "_id", task["_id"].Value<string>(),
                        "assembled", task["assembled"] = true
                    ).Async();
                    Debug("assembled true:\n" + res);
                }

                if (useCouchBaseReplication && !task["assembled"].Value<bool>() && fsConnector != null && !isCanceling)
                {
                    transferStatus.State = C8oFileTransferStatus.StateAssembling;
                    Notify(transferStatus);
                    //
                    // 2 : Gets the document describing the chunks list
                    //
                    var filepath = transferStatus.Filepath;
                    createdFileStream = fileManager.CreateFile(filepath);
                    createdFileStream.Position = 0;

                    for (int i = 0; i < transferStatus.Total; i++)
                    {
                        try
                        {
                            var meta = await c8o.CallJson("fs://" + fsConnector + ".get", "docid", uuid + "_" + i).Async();
                            Debug(meta.ToString());

                            AppendChunk(createdFileStream, meta.SelectToken("_attachments.chunk.content_url").ToString(), c8o);
                        }
                        catch (Exception e)
                        {
                            Debug("Failed to retrieve the attachment " + i + " due to: [" + e.GetType().Name + "] " + e.Message);
                            await DownloadChunk(c8o, createdFileStream, filepath, fsConnector, uuid, i, task, transferStatus);
                        }
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
                    res = await c8o.CallJson(".DeleteUuid", "uuid", uuid).Async();
                    Debug("deleteUuid:\n" + res);

                    res = await c8oTask.CallJson("fs://.post",
                        C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                        "_id", task["_id"].Value<string>(),
                        "remoteDeleted", task["remoteDeleted"] = true
                    ).Async();
                    Debug("remoteDeleted true:\n" + res);
                }

                if ((task["replicated"].Value<bool>() && task["assembled"].Value<bool>() && task["remoteDeleted"].Value<bool>()) || isCanceling)
                {
                    var res = await c8oTask.CallJson("fs://.delete", "docid", uuid).Async();
                    Debug("local delete:\n" + res.ToString());

                    transferStatus.State = C8oFileTransferStatus.StateFinished;
                    Notify(transferStatus);
                }

                if (isCanceling)
                {
                    transferStatus.State = C8oFileTransferStatus.StateCanceled;
                    Notify(transferStatus);
                    canceledTasks.Remove(uuid);
                }
            }
            catch (Exception e)
            {
                Notify(e);
            }
            finally
            {
                if (createdFileStream != null)
                {
                    createdFileStream.Dispose();
                }
                lock (_maxRunning)
                {
                    _maxRunning[0]++;
                    Monitor.Pulse(_maxRunning);
                }
            }

            if (needRemoveSession && c8o != null)
            {
                c8o.CallJson(".RemoveSession");
            }

            tasks.Remove(uuid);

            lock (this)
            {
                Monitor.Pulse(this);
            }
        }

        private async Task DownloadChunk(C8o c8o, Stream createdFileStream, string filepath, string fsConnector, string uuid, int i, JObject task, C8oFileTransferStatus transferStatus)
        {
            Stream chunkFS = null;
            var chunkpath = filepath + ".chunk";
            try
            {
                var fsurl = c8o.EndpointConvertigo + "/fullsync/" + fsConnector + "/" + uuid + "_" + i;
                var fsurlatt = fsurl + "/chunk";
                var digest = "no digest";

                int retry = 5;
                while (retry > 0)
                {
                    try
                    {
                        Debug("Getting the document at: " + fsurl);
                        var responseString = C8oTranslator.StreamToString(c8o.httpInterface.HandleGetRequest(fsurl).Result.GetResponseStream());

                        Debug("The document content: " + responseString);
                        var json = C8oTranslator.StringToJson(responseString);

                        digest = json["_attachments"]["chunk"]["digest"].ToString();
                        retry = 0;
                    }
                    catch (Exception e)
                    {
                        if (retry-- > 0)
                        {
                            Debug("Failed to get the chunk descriptor, retry. Cause: " + e.Message);
                        }
                        else
                        {
                            throw new Exception("Failed to get the chunk descriptor at " + fsurl, e);
                        }
                    }
                }

                chunkFS = fileManager.CreateFile(chunkpath);

                retry = 5;
                while (retry > 0)
                {
                    var md5 = "no md5";
                    try
                    {
                        Debug("Getting the attachment at: " + fsurlatt);
                        chunkFS.Position = 0;
                        AppendChunk(chunkFS, fsurlatt, c8o);

                        chunkFS.Position = 0;
                        md5 = "md5-" + c8o.GetMD5(chunkFS);
                    }
                    catch (Exception e)
                    {
                        Debug("Download Chunk failed, retry it. Cause: " + e.Message);
                    }

                    Debug("Comparing digests: " + digest + " / " + md5);

                    if (digest.Equals(md5))
                    {
                        chunkFS.Position = 0;
                        chunkFS.CopyTo(createdFileStream, 4096);
                        Debug("Chunk '" + uuid + "_" + i + "' assembled.");
                        retry = 0;
                        await c8oTask.CallJson("fs://.post",
                            C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                            "_id", task["_id"].Value<string>(),
                            "download", transferStatus.Current = i + 1,
                            "position", createdFileStream.Position
                        ).Async();
                        Notify(transferStatus);
                    }
                    else if (retry-- > 0)
                    {
                        Debug("The digest doesn't match, retry downloading.");
                    }
                    else
                    {
                        throw new Exception("Invalid digest: " + digest + " / " + md5);
                    }
                }
            }
            catch (Exception e2)
            {
                Debug("Failed to DownloadChunk, retry soon.");
                throw e2;
            }
            finally
            {
                if (chunkFS != null)
                {
                    chunkFS.Dispose();
                }
                fileManager.DeleteFile(chunkpath);
            }
        }

        private void AppendChunk(Stream createdFileStream, string contentPath, C8o c8o)
        {
            Stream chunkStream = null;
            try
            {
                if (contentPath.StartsWith("http://") || contentPath.StartsWith("https://"))
                {
                    var response = c8o.httpInterface.HandleGetRequest(contentPath, (int) MaxDurationForChunk.TotalMilliseconds).Result;
                    chunkStream = response.GetResponseStream();
                }
                else
                {
                    string contentPath2 = UrlToPath(contentPath);
                    chunkStream = fileManager.OpenFile(contentPath2);
                }
                Debug("AppendChunk for " + contentPath + " copy");
                chunkStream.CopyTo(createdFileStream, 4096);
                Debug("AppendChunk for " + contentPath + " copy finished");

                createdFileStream.Position = createdFileStream.Length;
            }
            finally
            {
                if (chunkStream != null)
                {
                    chunkStream.Dispose();
                }
            }
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
        /// Add a file to transfer to the upload queue.
        /// </summary>
        /// <param name="fileName">the name to identify the uploaded file</param>
        /// <param name="fileStream">an inputStream where the file to be uploaded is read</param>
        public async Task UploadFile(String fileName, Stream fileStream)
        {
            // Creates the task database if it doesn't exist
            CheckTaskDb();

            // Initializes the uuid ending with the number of chunks
            string uuid = System.Guid.NewGuid().ToString();

            // Posts a document describing the state of the upload in the task db
            await c8oTask.CallJson("fs://.post",
                 "_id", uuid,
                 "filePath", fileName,
                 "splitted", false,
                 "replicated", false,
                 "localDeleted", false,
                 "assembled", false,
                 "upload", 0,
                 "serverFilePath", ""
             ).Async();

            streamToUpload.Add(uuid, fileStream);

            // ???
            lock (this)
            {
                Monitor.Pulse(this);
            }
        }

        async Task UploadFile(C8oFileTransferStatus transferStatus, JObject task)
        {
            var uuid = transferStatus.Uuid;
            try
            {
                lock (_maxRunning)
                {
                    if (_maxRunning[0] <= 0)
                    {
                        Monitor.Wait(_maxRunning);
                    }
                    _maxRunning[0]--;
                }

                JObject res = null;
                bool[] locker = new bool[] { false };
                string fileName = transferStatus.Filepath; // task["fileName"].ToString();

                // Creates a c8o instance with a specific fullsync local suffix in order to store chunks in a specific database
                var c8o = new C8o(c8oTask.Endpoint, new C8oSettings(c8oTask).SetFullSyncLocalSuffix("_" + uuid).SetDefaultDatabaseName("c8ofiletransfer"));

                // Creates the local db
                await c8o.CallJson("fs://.create").Async();

                // If the file is not already splitted and stored in the local database
                if (!task["splitted"].Value<bool>() && !canceledTasks.Contains(uuid))
                {
                    transferStatus.State = C8oFileTransferStatus.StateSplitting;
                    Notify(transferStatus);

                    Stream fileStream;
                    if (!streamToUpload.TryGetValue(uuid, out fileStream))
                    {
                        // Removes the local database
                        await c8o.CallJson("fs://.reset").Async();
                        // Removes the task doc
                        await c8oTask.CallJson("fs://.delete", "docid", uuid).Async();
                        throw new Exception("The file '" + task["fileName"] + "' can't be upload because it was stopped before the file content was handled");
                    }

                    MemoryStream chunk = new MemoryStream(chunkSize);
                    fileStream.Position = 0;

                    //
                    // 1 : Split the file and store it locally
                    //
                    try
                    {
                        fileStream = streamToUpload[uuid];
                        byte[] buffer = new byte[chunkSize];
                        int countTot = -1;
                        int read = 1;
                        while (read > 0)
                        {
                            countTot++;

                            //fileStream.Position = chunkSize * countTot;
                            read = fileStream.Read(buffer, 0, chunkSize);
                            if (read > 0)
                            {
                                string docid = uuid + "_" + countTot;
                                await c8o.CallJson("fs://.post",
                                    "_id", docid,
                                    "fileName", fileName,
                                    "type", "chunk",
                                    "uuid", uuid
                                ).Async();

                                chunk = new MemoryStream(chunkSize);
                                // chunk.Position = 0;
                                chunk.Write(buffer, 0, read);

                                await c8o.CallJson("fs://.put_attachment",
                                    "docid", docid,
                                    "name", "chunk",
                                    "content_type", "application/octet-stream",
                                    "content", chunk).Async();

                                chunk.Dispose();
                            }

                        }
                        transferStatus.total = countTot;
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
                    Debug("splitted true:\n" + res.ToString());
                }

                streamToUpload.Remove(uuid);

                // If the local database is not replecated to the server
                if (!task["replicated"].Value<bool>() && !canceledTasks.Contains(uuid))
                {
                    //
                    // 2 : Authenticates
                    //
                    res = await c8o.CallJson(".SetAuthenticatedUser", "userId", uuid).Async();
                    Debug("SetAuthenticatedUser:\n" + res.ToString());

                    transferStatus.State = C8oFileTransferStatus.StateAuthenticated;
                    Notify(transferStatus);

                    //
                    // 3 : Replicates to server
                    //
                    transferStatus.State = C8oFileTransferStatus.StateReplicate;
                    Notify(transferStatus);

                    bool launchReplication = true;

                    // Relaunch replication while all documents are not replicated to the server
                    while (launchReplication)
                    {
                        locker[0] = false;
                        c8o.CallJson("fs://.replicate_push").Progress((c8oOnProgress) =>
                        {
                            if (c8oOnProgress.Finished)
                            {
                                // Checks if there is no more documents to replicate
                                if (c8oOnProgress.Total == 0)
                                {
                                    // Then the replication won't be launch again
                                    launchReplication = false;
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
                                Monitor.Wait(locker, 1000);
                            }

                            // Asks how many documents are in the server database with this uuid
                            JObject json = await c8o.CallJson(".c8ofiletransfer.GetViewCountByUuid", "_use_key", uuid).Async();
                            var item = json.SelectToken("document.couchdb_output.rows[0]");
                            if (item != null)
                            {
                                int current = item.Value<int>("value");
                                // If the number of documents has changed since the last time then notify
                                if (current != transferStatus.Current)
                                {
                                    transferStatus.Current = current;
                                    Notify(transferStatus);
                                }
                            }

                        } while (!locker[0] && !canceledTasks.Contains(uuid));

                        c8o.CallJson("fs://.replicate_push", "cancel", true).Sync();
                    }

                    if (!canceledTasks.Contains(uuid))
                    {
                        // Updates the state document in the task database
                        res = await c8oTask.CallJson("fs://.post",
                            C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                            "_id", task["_id"].Value<string>(),
                            "replicated", task["replicated"] = true
                        ).Async();
                        Debug("replicated true:\n" + res);
                    }
                }

                // If the local database containing chunks is not deleted
                locker[0] = true;
                if (!task["localDeleted"].Value<bool>())
                {
                    transferStatus.State = C8oFileTransferStatus.StateCleaning;
                    Notify(transferStatus);

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

                var isCanceling = canceledTasks.Contains(uuid);

                // If the file is not assembled in the server
                if (!task["assembled"].Value<bool>() && !isCanceling)
                {
                    transferStatus.State = C8oFileTransferStatus.StateAssembling;
                    Notify(transferStatus);

                    //
                    // 5 : Request the server to assemble chunks to the initial file
                    //
                    res = await c8o.CallJson(".StoreDatabaseFileToLocal",
                                             "uuid", uuid,
                                             "numberOfChunks", transferStatus.total).Async();
                    if (res.SelectToken("document.serverFilePath") == null)
                    {
                        throw new Exception("Can't find the serverFilePath in JSON response : " + res.ToString());
                    }
                    string serverFilePath = res.SelectToken("document").Value<string>("serverFilePath");
                    c8oTask.CallJson("fs://.post",
                            C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                            "_id", task["_id"].Value<string>(),
                            "assembled", task["assembled"] = true,
                            "serverFilePath", task["serverFilePath"] = serverFilePath
                        );
                    Debug("assembled true:\n" + res.ToString());
                }

                if (!isCanceling)
                {
                    transferStatus.ServerFilepath = task["serverFilePath"].ToString();

                    // Waits the local database is deleted
                    do
                    {
                        lock (locker)
                        {
                            Monitor.Wait(locker, 500);
                        }
                    } while (!locker[0]);
                }
                //
                // 6 : Remove the task document
                //
                res = await c8oTask.CallJson("fs://.delete", "docid", uuid).Async();
                Debug("local delete:\n" + res.ToString());

                if (isCanceling)
                {
                    canceledTasks.Remove(uuid);
                    transferStatus.State = C8oFileTransferStatus.StateCanceled;
                }
                else
                {
                    transferStatus.State = C8oFileTransferStatus.StateFinished;
                }
                Notify(transferStatus);
            }
            catch (Exception e)
            {
                Notify(e);
            }
            finally
            {
                lock (_maxRunning)
                {
                    _maxRunning[0]++;
                    Monitor.Pulse(_maxRunning);
                }
            }
        }

        /// <summary>
        /// List all the current transfers.
        /// </summary>
        /// <returns>list of all currents C8oFileTransferStatus</returns>
        public async Task<List<C8oFileTransferStatus>> GetAllFiletransferStatus()
        {
            var list = new List<C8oFileTransferStatus>();
            var res = await c8oTask.CallJson("fs://.all", "include_docs", true).Async();

            foreach (var row in (res["rows"] as JArray))
            {
                var task = row["doc"] as JObject;
                string uuid = task["_id"].ToString();

                // If this document id is not already in the tasks list
                if (tasks.ContainsKey(uuid))
                {
                    list.Add(tasks[uuid]);
                }
                else
                {
                    string filePath = task["filePath"].Value<string>();
                    list.Add(new C8oFileTransferStatus(uuid, filePath));
                }
            }
            return list;
        }

        /// <summary>
        /// Cancel a file transfer and clean local parts.
        /// </summary>
        /// <param name="filetransferStatus">the C8oFileTransferStatus of the transfer to interrupt</param>
        public async Task CancelFiletransfer(C8oFileTransferStatus filetransferStatus)
        {
            await CancelFiletransfer(filetransferStatus.Uuid);
        }

        /// <summary>
        /// Cancel a file transfer and clean local parts.
        /// </summary>
        /// <param name="uuid">the uuid of the transfer to interrupt</param>
        public async Task CancelFiletransfer(string uuid)
        {
            canceledTasks.Add(uuid);
        }

        /// <summary>
        /// Cancel all the file transfers.
        /// </summary>
        public async Task CancelFiletransfers()
        {
            foreach (var filetransferStatus in await GetAllFiletransferStatus())
            {
                await CancelFiletransfer(filetransferStatus);
            }
        }
    }
}
