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
using System.Net;

namespace C8oBigFileTransfer
{
    public class BigFileTransferInterface
    {
        private String endpoint;
        private C8oSettings c8oSettings;
        private FileManager fileManager;

        public BigFileTransferInterface(String endpoint, C8oSettings c8oSettings, FileManager fileManager)
        {
            this.endpoint = endpoint;
            this.c8oSettings = c8oSettings;
            this.fileManager = fileManager;
        }

        public async Task DownloadFile(String uuid, String filePath, Action<String> progress, Action<DownloadStatus> progressBis)
        {
            C8o c8o = new C8o(endpoint, c8oSettings);

            //
            // 0 : Authenticates the user on the Convertigo server in order to replicate wanted documents
            //

            JObject json = await c8o.CallJsonAsync(".SelectUuid", new Dictionary<String, Object>{{"uuid", uuid}});
            progress(json.ToString());
            
            //
            // 1 : Replicate the document discribing the chunks ids list
            //

            Boolean[] locker = new Boolean[] { false };

            c8o.Call("fs://.replicate_pull", null, new C8oJsonResponseListener((jsonResponse, requestParameters) =>
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
            }));

            // Waits the end of the replication if it is not finished
            lock (locker)
            {
                if (!locker[0])
                {
                    Monitor.Wait(locker);
                }
            }

            //
            // 2 : Gets the document describing the chunks list
            //

            JObject meta = await c8o.CallJsonAsync("fs://.get", new Dictionary<String, Object> { { "docid", uuid } });

            progress(meta.ToString());

            int chunks = (int) meta["chunks"];

            Stream createdFileStream = fileManager.CreateFile(filePath);
            createdFileStream.Position = 0;

            AppendChunk(createdFileStream, meta.SelectToken("_attachments.0.content_url").ToString());
            await c8o.CallJsonAsync("fs://.delete", new Dictionary<String, Object> { { "docid", uuid } });

            for (int i = 1; i < chunks; i++)
            {
                meta = await c8o.CallJsonAsync("fs://.get", new Dictionary<String, Object> { { "docid", uuid + "_" + i } });
                AppendChunk(createdFileStream, meta.SelectToken("_attachments." + i + ".content_url").ToString());
                await c8o.CallJsonAsync("fs://.delete", new Dictionary<String, Object> { { "docid", uuid + "_" + i } });
            }
            createdFileStream.Dispose();

            c8o.CallJsonAsync("fs://.replicate_push").Start();
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

    }
}
