using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Convertigo.SDK.Internal
{
    internal class C8oHTTPsProxy
    {
        static private int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = (listener.LocalEndpoint as IPEndPoint).Port;
            listener.Stop();
            return port;
        }

        static public void Init()
        {
            int randomPort = GetRandomUnusedPort();
            string proxyUrl = "http://localhost:" + randomPort + "/";
            var map = new Dictionary<string, Dictionary<string, object>>();
            int cpt = 0;
            var reUrl = new Regex("/(.*?)/(.*)");
            DateTime nextCheck = DateTime.Now.AddMinutes(10);
            var createReplication = C8oFullSyncDatabase.createReplication;

            C8oFullSyncDatabase.createReplication = (c8o, isPull, database, c8oFullSyncDatabaseUrl) =>
            {
                if (c8oFullSyncDatabaseUrl.Scheme == "https")
                {
                    var index = "" + cpt++;
                    var uri = new Uri(proxyUrl + index + "/");

                    lock (map)
                    {
                        map[index] = new Dictionary<string, object>()
                        {
                            { "FSURL", c8oFullSyncDatabaseUrl.AbsoluteUri},
                            { "C8O", c8o },
                            { "TTL", DateTime.Now.AddHours(1)}
                        };
                    }

                    var replication = isPull ?
                        database.CreatePullReplication(uri) :
                        database.CreatePushReplication(uri);

                    // Cookies
                    var cookies = c8o.CookieStore.GetCookies(new Uri(c8o.EndpointConvertigo));
                    foreach (Cookie cookie in cookies)
                    {
                        replication.SetCookie(cookie.Name, cookie.Value, "/", cookie.Expires, false, false);
                    }

                    return replication;
                }
                else
                {
                    return createReplication(c8o, isPull, database, c8oFullSyncDatabaseUrl);
                }
            };

            Task.Factory.StartNew(async () =>
            {
                var listener = new HttpListener();
                listener.Prefixes.Add(proxyUrl);
                listener.Start();

                while (true)
                {
                    var context = await listener.GetContextAsync();
                    Task.Run(async () =>
                    {
                        try
                        {
                            var matches = reUrl.Match(context.Request.RawUrl);
                            string index = matches.Groups[1].Value;
                            string c8oFsUrl = map[index]["FSURL"] as string;
                            var c8o = map[index]["C8O"] as C8o;
                            map[index]["TTL"] = DateTime.Now.AddHours(1);
                            var url = new Uri(c8oFsUrl + matches.Groups[2].Value);
                            var request = HttpWebRequest.Create(url) as HttpWebRequest;
                            c8o.httpInterface.OnRequestCreate(request);
                            request.Method = context.Request.HttpMethod;

                            Debug.WriteLine("\n<<< " + context.Request.HttpMethod + " " + url);
                            foreach (var name in context.Request.Headers.AllKeys)
                            {
                                try
                                {
                                    Debug.WriteLine("<<< " + name + "=" + context.Request.Headers[name]);
                                    if ("Accept".Equals(name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        request.Accept = context.Request.Headers[name];
                                    }
                                    else if ("Content-Type".Equals(name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        request.ContentType = context.Request.Headers[name];
                                    }
                                    else if (!"Host".Equals(name, StringComparison.OrdinalIgnoreCase)
                                        && !"Connection".Equals(name, StringComparison.OrdinalIgnoreCase)
                                        && !"Content-Length".Equals(name, StringComparison.OrdinalIgnoreCase)
                                        && !"Expect".Equals(name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        request.Headers[name] = context.Request.Headers[name];
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(name + " > add failed: " + ex);
                                }
                            }

                            if (context.Request.HasEntityBody)
                            {
                                await context.Request.InputStream.CopyToAsync(request.GetRequestStream());
                            }

                            HttpWebResponse response;
                            try
                            {
                                response = await request.GetResponseAsync() as HttpWebResponse;
                            }
                            catch (Exception e)
                            {
                                if (e is WebException)
                                {
                                    response = (e as WebException).Response as HttpWebResponse;
                                }
                                else if (e.InnerException is WebException)
                                {
                                    response = (e.InnerException as WebException).Response as HttpWebResponse;
                                }
                                else
                                {
                                    throw new Exception("boom", e);
                                }
                            }

                            foreach (var name in response.Headers.AllKeys)
                            {
                                try
                                {
                                    Debug.WriteLine(">>> " + name + "=" + response.Headers[name]);
                                    if (!"Transfer-Encoding".Equals(name, StringComparison.OrdinalIgnoreCase)
                                        && !"Content-Length".Equals(name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (response.Headers[name] != null && response.Headers[name].Length > 0)
                                        {
                                            context.Response.Headers[name] = response.Headers[name];
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(name + "< add failed: " + ex);
                                }
                            }

                            context.Response.StatusCode = (int) response.StatusCode;

                            await response.GetResponseStream().CopyToAsync(context.Response.OutputStream);
                            context.Response.OutputStream.Close();
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }

                        var now = DateTime.Now;
                        if (now > nextCheck)
                        {
                            lock (map)
                            {
                                var toRemove = new List<string>();
                                foreach (var entry in map)
                                {
                                    if (now > (DateTime) entry.Value["TTL"])
                                    {
                                        toRemove.Add(entry.Key);
                                    }
                                }
                                foreach (var key in toRemove)
                                {
                                    map.Remove(key);
                                }
                            }
                            nextCheck = now.AddMinutes(10);
                        }
                    }).GetAwaiter();
                }

            }, TaskCreationOptions.LongRunning);
        }
    }
}
