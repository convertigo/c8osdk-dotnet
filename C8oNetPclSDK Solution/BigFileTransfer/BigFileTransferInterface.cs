using Convertigo.SDK;
using Convertigo.SDK.Listeners;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Convertigo.SDK.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace BigFileTransfer2
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

        public void StoreFile(String fileId, String destinationPath)
        {
            List<String> chunkIdsList = new List<String>();
            // [0] : worked, [1] : finished 
            Boolean[] waiter = new Boolean[] { false, false };

            String requestable = "fs://" + this.databaseName + ".get";            
            Dictionary<String, Object> parameters = new Dictionary<string,object>();
            parameters.Add("docid", fileId);
            C8oJsonResponseListener responseListener = new C8oJsonResponseListener((jsonResponse, requestParameters) => 
            {
                String jsonStr = jsonResponse.ToString();
                JArray chunkIds;
                if (C8oUtils.TryGetValueAndCheckType<JArray>(jsonResponse, "chunkIds", out chunkIds))
                {
                    foreach (JToken chunkId in chunkIds) 
                    {
                        if (chunkId is JValue && (chunkId as JValue).Value is String)
                        {
                            chunkIdsList.Add((chunkId as JValue).Value as String);
                        }
                    }
                }

                long numberOfChunksToBeCreated;
                if (C8oUtils.TryGetValueAndCheckType<long>(jsonResponse, "numberOfChunksToBeCreated", out numberOfChunksToBeCreated))
                {
                    if (chunkIdsList.Count == numberOfChunksToBeCreated)
                    {
                        waiter[0] = true;
                    }
                }

                lock (waiter)
                {
                    waiter[1] = true;
                    Monitor.Pulse(waiter);
                }                
            });

            this.c8o.Call(requestable, parameters, responseListener);

            Task task = new Task(() =>
            {
                lock (waiter)
                {
                    // If the operation is not finished
                    if (!waiter[1])
                    {
                        Monitor.Wait(waiter);
                    }
                }

                List<String> contentPathes = new List<String>();

                foreach (String chunkIdStr in chunkIdsList)
                {
                    requestable = "fs://" + this.databaseName + ".get";
                    parameters = new Dictionary<string, object>();
                    parameters.Add("docid", chunkIdStr);

                    responseListener = new C8oJsonResponseListener((jsonResponse, requestParameters) =>
                    {
                        // Checks if there are attachments
                        JObject attachments;
                        if (C8oUtils.TryGetValueAndCheckType<JObject>(jsonResponse, "_attachments", out attachments))
                        {
                            String toto = attachments.ToString();

                            // 
                            JObject attachmentInfo;
                            if (C8oUtils.TryGetValueAndCheckType<JObject>(attachments, chunkIdStr, out attachmentInfo))
                            {
                                String contentPath;
                                if (C8oUtils.TryGetValueAndCheckType<String>(attachmentInfo, chunkIdStr, out contentPath))
                                {
                                    contentPathes.Add(contentPath);
                                }
                            }
                        }
                    });
                    this.c8o.Call(requestable, parameters, responseListener);

                    byte[][] chunksBytes;
                    long size = 0;
                    chunksBytes = new byte[contentPathes.Count][];
                    int i = 0;
                    foreach (String contentPath in contentPathes)
                    {
                        byte[] chunkByte = this.fileManager.ReadFile(contentPath);
                        size = size + chunkByte.Length;
                        chunksBytes[i] = chunkByte;
                        i++;
                    }

                    byte[] fileBytes = new byte[size];
                    int pos = 0;
                    foreach (byte[] chunkBytes in chunksBytes)
                    {
                        chunkBytes.CopyTo(fileBytes, pos);
                        pos = pos + chunkBytes.Length;
                    }

                    String t = "";

                }
            });
            task.Start();

            
            
        }




    }
}
