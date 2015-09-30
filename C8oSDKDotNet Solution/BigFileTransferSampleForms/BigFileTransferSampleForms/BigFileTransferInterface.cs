using Convertigo.SDK;
using Convertigo.SDK.Listeners;
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

namespace BigFileTransfer
{
    public class BigFileTransferInterface
    {

        private String databaseName;
        private C8o c8o;
        private FileManager fileManager;

        public BigFileTransferInterface(C8o c8o, String databaseName, FileManager fileManager)
        {
            this.c8o = c8o;
            this.databaseName = databaseName;
            this.fileManager = fileManager;
        }

        public async Task DownloadFile(String fileId, String filePath, Action<String> progress, Action<DownloadStatus> progressBis)
        {
            String reqRepPull = "fs://" + this.databaseName + ".replicate_pull";
            String reqGetDoc = "fs://" + this.databaseName + ".get";
            Exception exception = null;

            //
            // 0 : Authenticates the user on the Convertigo server in order to replicate wanted documents
            //

            Boolean[] locker = new Boolean[] { false };
            String reqAuth = "BigFileTransfer.Authenticate";
            Dictionary<String, Object> parameters = new Dictionary<string, object>();
            parameters.Add("userId", fileId);
            C8oJsonResponseListener responseListener = new C8oJsonResponseListener((jsonResponse, requestParameters) =>
            {
                String authResponse = jsonResponse.ToString();
                String aaa = "";
                lock (locker)
                {
                    locker[0] = true;
                    Monitor.Pulse(locker);
                }
            });
            c8o.Call(reqAuth, parameters, responseListener);

            // Waits the end of the replication if it is not finished
            lock (locker)
            {
                if (!locker[0])
                {
                    Monitor.Wait(locker);
                }
            }

            //
            // 1 : Replicate the document discribing the chunks ids list
            //
            locker = new Boolean[] { false };
            parameters = new Dictionary<string, object>();
            responseListener = new C8oJsonResponseListener((jsonResponse, requestParameters) =>
            {

                progress(jsonResponse.ToString());

                int current = -1;
                int total = -1;
                C8oUtils.TryGetValueAndCheckType<int>(jsonResponse, "current", out current);
                C8oUtils.TryGetValueAndCheckType<int>(jsonResponse, "total", out total);
                DownloadStatus dlStatus = new DownloadStatus(current, total);
                progressBis(dlStatus);


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
            });
            Task task = new Task(async () =>
            {
                try
                {
                    c8o.Call(reqRepPull, parameters, responseListener);
                }
                catch (Exception e)
                {
                    exception = e;
                }
            });
            task.Start();

            // Waits the end of the replication if it is not finished
            lock (locker)
            {
                if (!locker[0])
                {
                    Monitor.Wait(locker);
                }
            }

            if (exception != null)
            {
                throw exception;
            }

            //
            // 2 : Gets the document describing the chunks list
            //

            locker = new Boolean[] { false };

            List<String> chunkIdsList = new List<String>();
            parameters = new Dictionary<string, object>();
            parameters.Add("docid", fileId);
            responseListener = new C8oJsonResponseListener((jsonResponse, requestParameters) =>
            {
                progress(jsonResponse.ToString());

                String errorMessage = "Fields are missing";
                try
                {
                    JArray chunkIds;
                    if (C8oUtils.TryGetValueAndCheckType<JArray>(jsonResponse, "chunkIds", out chunkIds))
                    {
                        // Chunks are replicated from the database with the same order as they are stored into the database
                        // So, merge chunks in this order will work
                        foreach (JToken chunkId in chunkIds)
                        {
                            if (chunkId is JValue && (chunkId as JValue).Value is String)
                            {
                                chunkIdsList.Add((chunkId as JValue).Value as String);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(errorMessage + " : chunksId");
                    }

                    long numberOfChunksToBeCreated;
                    if (C8oUtils.TryGetValueAndCheckType<long>(jsonResponse, "numberOfChunksToBeCreated", out numberOfChunksToBeCreated))
                    {
                        if (chunkIdsList.Count != numberOfChunksToBeCreated)
                        {
                            throw new Exception("Invalid number of chunks");
                        }
                    }
                    else
                    {
                        throw new Exception(errorMessage + " : numberOfChunksToBeCreated");
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
                lock (locker)
                {
                    Monitor.Pulse(locker);
                }
            });
            try
            {
                this.c8o.Call("fs://" + this.databaseName + ".all", parameters, responseListener);
            }
            catch (Exception e)
            {
                throw e;
            }
            lock (locker)
            {
                if (!locker[0])
                {
                    Monitor.Wait(locker);
                }
            }
            if (exception != null)
            {
                throw exception;
            }

            // Waits the end of the replication if it is not finished
            // Chunks could start to be merged here
            

            //
            // 4 : Gets documents containing chunks as attachment and merge chunks into one file
            //
            List<String> contentPathes = new List<String>();
            foreach (String chunkId in chunkIdsList)
            {
                parameters = new Dictionary<string, object>();
                parameters.Add("docid", chunkId);

                responseListener = new C8oJsonResponseListener((jsonResponse, requestParameters) =>
                {
                    // Checks if there are attachments
                    JObject attachments;
                    if (C8oUtils.TryGetValueAndCheckType<JObject>(jsonResponse, "_attachments", out attachments))
                    {
                        JObject attachmentInfo;
                        if (C8oUtils.TryGetValueAndCheckType<JObject>(attachments, chunkId, out attachmentInfo))
                        {
                            String contentPath;
                            if (C8oUtils.TryGetValueAndCheckType<String>(attachmentInfo, "content_path", out contentPath))
                            {
                                contentPathes.Add(contentPath);
                            }
                        }
                    }
                });
                c8o.Call(reqGetDoc, parameters, responseListener);
            }
            try
            {
                Stream createdFileStream = fileManager.CreateFile(filePath);
                createdFileStream.Position = 0;
                foreach (String contentPath in contentPathes)
                {
                    String contentPath2 = UrlToPath(contentPath);
                    Stream chunkFileStream = fileManager.OpenFile(contentPath2);
                    chunkFileStream.CopyTo(createdFileStream, 4096);
                    chunkFileStream.Dispose();
                    createdFileStream.Position = createdFileStream.Length;
                }
                createdFileStream.Dispose();
            }
            catch (Exception e)
            {
                throw e;
            }
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

    }
}
