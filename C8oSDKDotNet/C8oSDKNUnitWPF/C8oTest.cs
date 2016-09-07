using Convertigo.SDK;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace C8oSDKNUnitWPF
{

    [TestFixture]
    class C8oTest
    {
        static readonly string HOST = "buildus.twinsoft.fr";
        static readonly string PROJECT_PATH = "/convertigo/projects/ClientSDKtesting";

        class Stuff
        {
            internal static readonly IDictionary<Stuff, object> stuffs = new Dictionary<Stuff, object>();

            internal static readonly Stuff C8O = new Stuff(() =>
            {
                C8o c8o = new C8o("http://" + HOST + ":28080" + PROJECT_PATH);
                c8o.LogRemote = false;
                c8o.LogLevelLocal = C8oLogLevel.ERROR;
                return c8o;
            });

            internal static readonly Stuff C8O_BIS = new Stuff(() =>
            {
                return C8O.get();
            });

            internal static readonly Stuff C8O_FS = new Stuff(() =>
            {
                C8o c8o = new C8o("http://" + HOST + ":28080" + PROJECT_PATH, new C8oSettings()
                    .SetDefaultDatabaseName("clientsdktesting")
                );
                c8o.LogRemote = false;
                c8o.LogLevelLocal = C8oLogLevel.ERROR;
                return c8o;
            });

            internal static readonly Stuff C8O_FS_PULL = new Stuff(() =>
            {
                C8o c8o = new C8o("http://" + HOST + ":28080" + PROJECT_PATH, new C8oSettings()
                    .SetDefaultDatabaseName("qa_fs_pull")
                );
                c8o.LogRemote = false;
                c8o.LogLevelLocal = C8oLogLevel.ERROR;
                var json = c8o.CallJson(".InitFsPull").Sync();
                Assert.IsTrue(json.SelectToken("document.ok").Value<bool>());
                return c8o;
            });

            internal static readonly Stuff C8O_FS_PUSH = new Stuff(() =>
            {
                C8o c8o = new C8o("http://" + HOST + ":28080" + PROJECT_PATH, new C8oSettings()
                    .SetDefaultDatabaseName("qa_fs_push")
                );
                c8o.LogRemote = false;
                c8o.LogLevelLocal = C8oLogLevel.ERROR;
                var json = c8o.CallJson(".InitFsPush").Sync();
                Assert.IsTrue(json.SelectToken("document.ok").Value<bool>());
                return c8o;
            });

            internal static readonly Stuff C8O_LC = new Stuff(() =>
            {
                C8o c8o = new C8o("http://" + HOST + ":28080" + PROJECT_PATH);
                c8o.LogRemote = false;
                c8o.LogLevelLocal = C8oLogLevel.ERROR;
                return c8o;
            });

            internal static readonly Stuff SetGetInSession = new Stuff(() =>
            {
                C8o c8o = Get<C8o>(C8O_BIS);
                var ts = "" + DateTime.Now.Ticks;
                var doc = c8o.CallXml(".SetInSession", "ts", ts).Sync();
                var newTs = doc.XPathSelectElement("/document/pong/ts").Value;
                Assert.AreEqual(ts, newTs);
                doc = c8o.CallXml(".GetFromSession").Sync();
                newTs = doc.XPathSelectElement("/document/session/expression").Value;
                Assert.AreEqual(ts, newTs);
                return new object();
            });

            private Stuff(Func<object> get)
            {
                this.get = get;
            }

            Func<object> get;

            internal static T Get<T>(Stuff stuff)
            {
                lock (stuff)
                {
                    object res = stuffs.ContainsKey(stuff) ? stuffs[stuff] : null;
                    if (res == null)
                    {
                        try
                        {
                            res = stuff.get();
                        }
                        catch (Exception e)
                        {
                            res = e;
                        }
                        stuffs[stuff] = res;
                    }
                    if (res is Exception)
                    {
                        throw res as Exception;
                    }

                    T t = (T) res;

                    return t;
                }
            }
        }

        Queue<Action> uiQueue = new Queue<Action>();

        T Get<T>(Stuff stuff)
        {
            return Stuff.Get<T>(stuff);
        }

        [SetUp]
        public void SetUp()
        {
            // before any tests
            new Thread(() =>
            {
                Thread.CurrentThread.Name = "FakeUI";
                while (Thread.CurrentThread.IsAlive)
                {
                    lock (uiQueue)
                    {
                        Monitor.Wait(uiQueue, 1000);
                    }
                    if (uiQueue.Count > 0)
                    {
                        uiQueue.Dequeue()();
                    }
                }
            }).Start();

            C8oPlatform.Init(action =>
            {
                uiQueue.Enqueue(action);
                lock (uiQueue)
                {
                    Monitor.Pulse(uiQueue);
                }
            });
        }

        [Test]
        public void C8oBadEndpoint()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new C8o("http://" + HOST + ":28080");
            });
        }

        [Test]
        public void C8oDefault()
        {
            Get<C8o>(Stuff.C8O);
        }

        [Test]
        public void C8oDefaultPing()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var doc = c8o.CallXml(".Ping").Sync();
            var pong = doc.XPathSelectElement("/document/pong");
            Assert.NotNull(pong);
        }

        [Test]
        public void C8oDefaultPingWait()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var promise = c8o.CallXml(".Ping");
            Thread.Sleep(500);
            var doc = promise.Sync();
            var pong = doc.XPathSelectElement("/document/pong");
            Assert.NotNull(pong);
        }

        [Test]
        public void C8oCallInAsyncTask()
        {
            XDocument doc = null;
            Task.Run(() => {
                var c8o = Get<C8o>(Stuff.C8O);
                doc = c8o.CallXml(".Ping").Sync();
            }).Wait();
            var pong = doc.XPathSelectElement("/document/pong");
            Assert.NotNull(pong);
        }

        [Test]
        public async Task C8oDefaultPingAsync()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var doc = await c8o.CallXml(".Ping").Async();
            var pong = doc.XPathSelectElement("/document/pong");
            Assert.NotNull(pong);
        }

        [Test]
        public void C8oUnknownHostCallAndLog()
        {
            var exception = null as Exception;
            var exceptionLog = null as Exception;
            var c8o = new C8o("http://" + HOST + "ee:28080" + PROJECT_PATH, new C8oSettings()
                .SetLogOnFail((ex, parameters) =>
                {
                    exceptionLog = ex;
                })
            );
            c8o.Log.Warn("must fail log");
            Thread.Sleep(250);
            try
            {
                c8o.CallXml(".Ping").Sync();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.NotNull(exception);
            Assert.AreEqual("Convertigo.SDK.C8oException", exception.GetType().FullName);
            exception = exception.InnerException;
            Assert.AreEqual("System.AggregateException", exception.GetType().FullName);
            exception = exception.InnerException;
            Assert.AreEqual("System.Net.WebException", exception.GetType().FullName);
            Assert.NotNull(exceptionLog);
            Assert.AreEqual("Convertigo.SDK.C8oException", exceptionLog.GetType().FullName);
            exceptionLog = exceptionLog.InnerException;
            Assert.AreEqual("System.AggregateException", exceptionLog.GetType().FullName);
            exceptionLog = exceptionLog.InnerException;
            Assert.AreEqual("System.Net.WebException", exceptionLog.GetType().FullName);
        }

        [Test]
        public void C8oUnknownHostCallWait()
        {
            var exception = null as Exception;
            var c8o = new C8o("http://" + HOST + "ee:28080" + PROJECT_PATH);
            try
            {
                var promise = c8o.CallXml(".Ping");
                Thread.Sleep(500);
                promise.Sync();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.NotNull(exception);
            Assert.AreEqual("Convertigo.SDK.C8oException", exception.GetType().FullName);
            exception = exception.InnerException;
            Assert.AreEqual("System.AggregateException", exception.GetType().FullName);
            exception = exception.InnerException;
            Assert.AreEqual("System.Net.WebException", exception.GetType().FullName);
        }

        [Test]
        public void C8oDefaultPingOneSingleValue()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var doc = c8o.CallXml(".Ping", "var1", "value one").Sync();
            var value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual("value one", value);
        }

        [Test]
        public void C8oDefaultPingTwoSingleValues()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var doc = c8o.CallXml(".Ping",
                "var1", "value one",
                "var2", "value two"
            ).Sync();
            var value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual("value one", value);
            value = doc.XPathSelectElement("/document/pong/var2").Value;
            Assert.AreEqual("value two", value);
        }

        [Test]
        public void C8oDefaultPingTwoSingleValuesOneMulti()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var doc = c8o.CallXml(".Ping",
                "var1", "value one",
                "var2", "value two",
                "mvar1", new string[] { "mvalue one", "mvalue two", "mvalue three" }
            ).Sync();
            object value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual("value one", value);
            value = doc.XPathSelectElement("/document/pong/var2").Value;
            Assert.AreEqual("value two", value);
            value = doc.XPathSelectElement("/document/pong/mvar1[1]").Value;
            Assert.AreEqual("mvalue one", value);
            value = doc.XPathSelectElement("/document/pong/mvar1[2]").Value;
            Assert.AreEqual("mvalue two", value);
            value = doc.XPathSelectElement("/document/pong/mvar1[3]").Value;
            Assert.AreEqual("mvalue three", value);
            value = doc.XPathEvaluate("count(/document/pong/mvar1)");
            Assert.AreEqual(3.0, value);
        }

        [Test]
        public void C8oDefaultPingTwoSingleValuesTwoMulti()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var doc = c8o.CallXml(".Ping",
                "var1", "value one",
                "var2", "value two",
                "mvar1", new string[] { "mvalue one", "mvalue two", "mvalue three" },
                "mvar2", new string[] { "mvalue2 one" }
            ).Sync();
            object value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual("value one", value);
            value = doc.XPathSelectElement("/document/pong/var2").Value;
            Assert.AreEqual("value two", value);
            value = doc.XPathSelectElement("/document/pong/mvar1[1]").Value;
            Assert.AreEqual("mvalue one", value);
            value = doc.XPathSelectElement("/document/pong/mvar1[2]").Value;
            Assert.AreEqual("mvalue two", value);
            value = doc.XPathSelectElement("/document/pong/mvar1[3]").Value;
            Assert.AreEqual("mvalue three", value);
            value = doc.XPathEvaluate("count(/document/pong/mvar1)");
            Assert.AreEqual(3.0, value);
            value = doc.XPathSelectElement("/document/pong/mvar2[1]").Value;
            Assert.AreEqual("mvalue2 one", value);
            value = doc.XPathEvaluate("count(/document/pong/mvar2)");
            Assert.AreEqual(1.0, value);
        }

        [Test]
        public void C8oCheckJsonTypes()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var json = c8o.CallJson(".JsonTypes",
                "var1", "value one",
                "mvar1", new string[] { "mvalue one", "mvalue two", "mvalue three" }
            ).Sync();
            object value = json.SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("value one", value);
            value = json.SelectToken("document.pong.mvar1[0]").ToString();
            Assert.AreEqual("mvalue one", value);
            value = json.SelectToken("document.pong.mvar1[1]").ToString();
            Assert.AreEqual("mvalue two", value);
            value = json.SelectToken("document.pong.mvar1[2]").ToString();
            Assert.AreEqual("mvalue three", value);
            value = json.SelectToken("document.pong.mvar1").ToObject<JArray>().Count;
            Assert.AreEqual(3, value);
            value = json.SelectToken("document.complex.isNull").ToObject<object>();
            Assert.IsNull(value);
            value = json.SelectToken("document.complex.isInt3615").ToObject<int>();
            Assert.AreEqual(3615, value);
            value = json.SelectToken("document.complex.isStringWhere").ToObject<string>();
            Assert.AreEqual("where is my string?!", value);
            value = json.SelectToken("document.complex.isDoublePI").ToObject<double>();
            Assert.AreEqual(3.141592653589793, value);
            var isBool = json.SelectToken("document.complex.isBoolTrue").ToObject<bool>();
            Assert.IsTrue(isBool);
            value = json.SelectToken("document.complex.ÉlŸz@-node").ToObject<string>();
            Assert.AreEqual("that's ÉlŸz@", value);
        }
        
        [Test]
        public void SetGetInSession()
        {
            Get<object>(Stuff.SetGetInSession);
        }

        [Test]
        public void CheckNoMixSession()
        {
            Get<object>(Stuff.SetGetInSession);
            var c8o = Get<C8o>(Stuff.C8O);
            var doc = c8o.CallXml(".GetFromSession").Sync();
            var expression = doc.XPathSelectElement("/document/session/expression");
            Assert.IsNull(expression);
        }

        private void CheckLogRemoteHelper(C8o c8o, string lvl, string msg)
        {
            Thread.Sleep(333);
            var doc = c8o.CallXml(".GetLogs").Sync();
            var elt = doc.XPathSelectElement("/document/line");
            Assert.NotNull(elt, lvl);
            var sLine = elt.Value;
            Assert.True(sLine != null && sLine.Length > 0, "[" + lvl + "] sLine='" + sLine + "'");
            var line = JArray.Parse(sLine);
            Assert.AreEqual(lvl, line[2].ToString());
            var newMsg = line[4].ToString();
            newMsg = newMsg.Substring(newMsg.IndexOf("logID="));
            Assert.AreEqual(msg, newMsg);
        }

        [Test]
        public void CheckLogRemote()
        {
            var c8o = new C8o("http://" + HOST + ":28080" + PROJECT_PATH);
            c8o.LogC8o = false;
            var id = "logID=" + DateTime.Now.Ticks;
            c8o.CallXml(".GetLogs", "init", id).Sync();
            Thread.Sleep(333);
            c8o.Log.Error(id);
            CheckLogRemoteHelper(c8o, "ERROR", id);
            c8o.Log.Error(id, new C8oException("for test"));
            CheckLogRemoteHelper(c8o, "ERROR", id + "\nConvertigo.SDK.C8oException: for test");
            c8o.Log.Warn(id);
            CheckLogRemoteHelper(c8o, "WARN", id);
            c8o.Log.Info(id);
            CheckLogRemoteHelper(c8o, "INFO", id);
            c8o.Log.Debug(id);
            CheckLogRemoteHelper(c8o, "DEBUG", id);
            c8o.Log.Trace(id);
            CheckLogRemoteHelper(c8o, "TRACE", id);
            c8o.Log.Fatal(id);
            CheckLogRemoteHelper(c8o, "FATAL", id);
            c8o.LogRemote = false;
            c8o.Log.Info(id);
            Thread.Sleep(333);
            var doc = c8o.CallXml(".GetLogs").Sync();
            object value = doc.XPathSelectElement("/document/line");
            Assert.Null(value);
        }

        [Test]
        public void C8oDefaultPromiseXmlOne()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xdoc = new XDocument[1];
            var xthread = new Thread[1];
            var xparam = new IDictionary<string, object>[1];

            lock (xdoc)
            {
                c8o.CallXml(".Ping", "var1", "step 1").Then((doc, param) =>
                {
                    xdoc[0] = doc;
                    xthread[0] = Thread.CurrentThread;
                    xparam[0] = param;

                    lock (xdoc)
                    {
                        Monitor.Pulse(xdoc);
                    }
                    return null;
                });
                Monitor.Wait(xdoc, 5000);
            }
            object value = xdoc[0].XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual("step 1", value);
            Assert.AreNotEqual(Thread.CurrentThread, xthread[0]);
            Assert.AreEqual("step 1", xparam[0]["var1"]);
        }

        [Test]
        public void C8oDefaultPromiseJsonThree()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[3];

            lock (xjson)
            {
                c8o.CallJson(".Ping", "var1", "step 1").Then((json, param) =>
                {
                    xjson[0] = json;
                    return c8o.CallJson(".Ping", "var1", "step 2");
                }).Then((json, param) =>
                {
                    xjson[1] = json;
                    return c8o.CallJson(".Ping", "var1", "step 3");
                }).Then((json, param) =>
                {
                    xjson[2] = json;
                    lock (xjson)
                    {
                        Monitor.Pulse(xjson);
                    }
                    return null;
                });
                Monitor.Wait(xjson, 5000);
            }
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
            value = xjson[2].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 3", value);
        }

        [Test]
        public void C8oDefaultPromiseUI()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[3];
            var xthread = new Thread[3];

            lock (xjson)
            {
                c8o.CallJson(".Ping", "var1", "step 1").ThenUI((json, param) =>
                {
                    xjson[0] = json;
                    xthread[0] = Thread.CurrentThread;
                    return c8o.CallJson(".Ping", "var1", "step 2");
                }).Then((json, param) =>
                {
                    xjson[1] = json;
                    xthread[1] = Thread.CurrentThread;
                    return c8o.CallJson(".Ping", "var1", "step 3");
                }).ThenUI((json, param) =>
                {
                    xjson[2] = json;
                    xthread[2] = Thread.CurrentThread;
                    lock (xjson)
                    {
                        Monitor.Pulse(xjson);
                    }
                    return null;
                });
                Monitor.Wait(xjson, 5000);
            }
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
            value = xjson[2].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 3", value);
            Assert.AreEqual("FakeUI", xthread[0].Name);
            Assert.AreNotEqual("FakeUI", xthread[1].Name);
            Assert.AreEqual("FakeUI", xthread[2].Name);
        }

        [Test]
        public void C8oDefaultPromiseFail()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[3];
            var xfail = new Exception[1];
            var xparam = new IDictionary<string, object>[1];

            lock (xjson)
            {
                c8o.CallJson(".Ping", "var1", "step 1").Then((json, param) =>
                {
                    xjson[0] = json;
                    return c8o.CallJson(".Ping", "var1", "step 2");
                }).Then((json, param) =>
                {
                    xjson[1] = json;
                    if (json != null)
                    {
                        throw new C8oException("random failure");
                    }
                    return c8o.CallJson(".Ping", "var1", "step 3");
                }).Then((json, param) =>
                {
                    xjson[2] = json;
                    lock (xjson)
                    {
                        Monitor.Pulse(xjson);
                    }
                    return null;
                }).Fail((ex, param) =>
                {
                    xfail[0] = ex;
                    xparam[0] = param;
                    lock (xjson)
                    {
                        Monitor.Pulse(xjson);
                    }
                });
                Monitor.Wait(xjson, 5000);
            }
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
            Assert.IsNull(xjson[2]);
            Assert.AreEqual("random failure", xfail[0].Message);
            Assert.AreEqual("step 2", xparam[0]["var1"]);
        }

        [Test]
        public void C8oDefaultPromiseFailUI()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[3];
            var xfail = new Exception[1];
            var xparam = new IDictionary<string, object>[1];
            var xthread = new Thread[1];

            lock (xjson)
            {
                c8o.CallJson(".Ping", "var1", "step 1").Then((json, param) =>
                {
                    xjson[0] = json;
                    return c8o.CallJson(".Ping", "var1", "step 2");
                }).Then((json, param) =>
                {
                    xjson[1] = json;
                    if (json != null)
                    {
                        throw new C8oException("random failure");
                    }
                    return c8o.CallJson(".Ping", "var1", "step 3");
                }).Then((json, param) =>
                {
                    xjson[2] = json;
                    lock (xjson)
                    {
                        Monitor.Pulse(xjson);
                    }
                    return null;
                }).FailUI((ex, param) =>
                {
                    xfail[0] = ex;
                    xparam[0] = param;
                    xthread[0] = Thread.CurrentThread;
                    lock (xjson)
                    {
                        Monitor.Pulse(xjson);
                    }
                });
                Monitor.Wait(xjson, 5000);
            }
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
            Assert.IsNull(xjson[2]);
            Assert.AreEqual("random failure", xfail[0].Message);
            Assert.AreEqual("step 2", xparam[0]["var1"]);
            Assert.AreEqual("FakeUI", xthread[0].Name);
        }

        [Test]
        public void C8oDefaultPromiseSync()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[2];
            xjson[1] = c8o.CallJson(".Ping", "var1", "step 1").Then((json, param) =>
            {
                xjson[0] = json;
                return c8o.CallJson(".Ping", "var1", "step 2");
            }).Sync();
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
        }

        [Test]
        public void C8oDefaultPromiseSyncFail()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[2];
            var exception = null as Exception;
            try
            {
                xjson[1] = c8o.CallJson(".Ping", "var1", "step 1").Then((json, param) =>
                {
                    xjson[0] = json;
                    if (json != null)
                    {
                        throw new C8oException("random failure");
                    }
                    return c8o.CallJson(".Ping", "var1", "step 2");
                }).Sync();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            Assert.Null(xjson[1]);
            Assert.NotNull(exception);
            Assert.AreEqual("random failure", exception.Message);
        }

        [Test]
        public void C8oDefaultPromiseNested()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[6];
            xjson[5] = c8o.CallJson(".Ping", "var1", "step 1").Then((json, param) =>
            {
                xjson[0] = json;
                return c8o.CallJson(".Ping", "var1", "step 2").Then((json2, param2) =>
                {
                    xjson[1] = json2;
                    return c8o.CallJson(".Ping", "var1", "step 3").Then((json3, param3) =>
                    {
                        xjson[2] = json3;
                        return c8o.CallJson(".Ping", "var1", "step 4");
                    });
                });
            }).Then((json, param) =>
            {
                xjson[3] = json;
                return c8o.CallJson(".Ping", "var1", "step 5").Then((json2, param2) =>
                {
                    xjson[4] = json2;
                    return null;
                });
            }).Sync();
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
            value = xjson[2].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 3", value);
            value = xjson[3].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 4", value);
            value = xjson[4].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 5", value);
            value = xjson[5].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 5", value);
        }

        [Test]
        public void C8oDefaultPromiseNestedFail()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[6];
            var xfail = new Exception[2];
            try {
                xjson[5] = c8o.CallJson(".Ping", "var1", "step 1").Then((json, param) =>
                {
                    xjson[0] = json;
                    return c8o.CallJson(".Ping", "var1", "step 2").Then((json2, param2) =>
                    {
                        xjson[1] = json2;
                        return c8o.CallJson(".Ping", "var1", "step 3").Then((json3, param3) =>
                        {
                            xjson[2] = json3;
                            throw new C8oException("random failure");
                        });
                    });
                }).Then((json, param) =>
                {
                    xjson[3] = json;
                    return c8o.CallJson(".Ping", "var1", "step 5").Then((json2, param2) =>
                    {
                        xjson[4] = json2;
                        return null;
                    });
                }).Fail((e, param) =>
                {
                    xfail[0] = e;
                }).Sync();
            } catch (Exception e) {
                xfail[1] = e;
            }
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
            value = xjson[2].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 3", value);
            value = xjson[3];
            Assert.Null(value);
            value = xjson[4];
            Assert.Null(value);
            value = xjson[5];
            Assert.Null(value);
            Assert.AreEqual("random failure", xfail[0].Message);
            Assert.AreEqual(xfail[0], xfail[1]);
        }

        [Test]
        public void C8oDefaultPromiseInVar()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[3];
            var promise = c8o.CallJson(".Ping", "var1", "step 1");
            promise.Then((json, param) =>
            {
                xjson[0] = json;
                return c8o.CallJson(".Ping", "var1", "step 2");
            });
            promise.Then((json, param) =>
            {
                xjson[1] = json;
                return c8o.CallJson(".Ping", "var1", "step 3");
            });
            xjson[2] = promise.Sync();
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
            value = xjson[2].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 3", value);
        }

        [Test]
        public void C8oDefaultPromiseInVarSleep()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            var xjson = new JObject[3];
            var promise = c8o.CallJson(".Ping", "var1", "step 1");
            Thread.Sleep(500);
            promise.Then((json, param) =>
            {
                xjson[0] = json;
                return c8o.CallJson(".Ping", "var1", "step 2");
            });
            Thread.Sleep(500);
            promise.Then((json, param) =>
            {
                xjson[1] = json;
                return c8o.CallJson(".Ping", "var1", "step 3");
            });
            Thread.Sleep(500);
            xjson[2] = promise.Sync();
            object value = xjson[0].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 1", value);
            value = xjson[1].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 2", value);
            value = xjson[2].SelectToken("document.pong.var1").ToString();
            Assert.AreEqual("step 3", value);
        }

        [Test]
        public void C8o0Ssl1TrustFail()
        {
            Exception exception = null;
            try
            {
                var c8o = new C8o("https://" + HOST + ":443" + PROJECT_PATH);
                var doc = c8o.CallXml(".Ping", "var1", "value one").Sync();
                var value = doc.XPathSelectElement("/document/pong/var1").Value;
                Assert.True(false, "not possible");
            }
            catch (AssertionException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.NotNull(exception);
            Assert.AreEqual("Convertigo.SDK.C8oException", exception.GetType().FullName);
            exception = exception.InnerException;
            Assert.AreEqual("System.Net.WebException", exception.GetType().FullName);
            exception = exception.InnerException;
            Assert.AreEqual("System.Security.Authentication.AuthenticationException", exception.GetType().FullName);
        }

        [Test]
        public void C8o0Ssl2TrustAll()
        {
            var c8o = new C8o("https://" + HOST + ":443" + PROJECT_PATH, new C8oSettings().SetTrustAllCertificates(true));
            var doc = c8o.CallXml(".Ping", "var1", "value one").Sync();
            var value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual("value one", value);
            var request = WebRequest.Create(c8o.EndpointConvertigo);
            request.GetResponse().Close();
        }

        [Test]
        public void C8oFsPostGetDelete()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var myId = "C8oFsPostGetDelete-" + DateTime.Now.Ticks;
                json = c8o.CallJson("fs://.post", "_id", myId).Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = json["id"].Value<string>();
                Assert.AreEqual(myId, id);
                json = c8o.CallJson("fs://.get", "docid", id).Sync();
                id = json["_id"].Value<string>();
                Assert.AreEqual(myId, id);
                json = c8o.CallJson("fs://.delete", "docid", id).Sync();
                Assert.True(json["ok"].Value<bool>());
                try
                {
                    c8o.CallJson("fs://.get", "docid", id).Sync();
                    Assert.True(false, "not possible");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Convertigo.SDK.C8oRessourceNotFoundException", e.GetType().FullName);
                }
            }
        }

        [Test]
        public void C8oFsPostGetDeleteRev()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = "C8oFsPostGetDeleteRev-" + DateTime.Now.Ticks;
                json = c8o.CallJson("fs://.post", "_id", id).Sync();
                Assert.True(json["ok"].Value<bool>());
                var rev = json["rev"].Value<string>();
                try
                {
                    c8o.CallJson("fs://.delete", "docid", id, "rev", "1-123456").Sync();
                    Assert.True(false, "not possible");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Convertigo.SDK.C8oRessourceNotFoundException", e.GetType().FullName);
                }
                json = c8o.CallJson("fs://.delete", "docid", id, "rev", rev).Sync();
                Assert.True(json["ok"].Value<bool>());
                try
                {
                    c8o.CallJson("fs://.get", "docid", id).Sync();
                    Assert.True(false, "not possible");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Convertigo.SDK.C8oRessourceNotFoundException", e.GetType().FullName);
                }
            }
        }

        [Test]
        public void C8oFsPostGetDestroyCreate()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var ts = "ts=" + DateTime.Now.Ticks;
                var ts2 = ts + "@test";
                json = c8o.CallJson("fs://.post", "ts", ts).Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = json["id"].Value<string>();
                var rev = json["rev"].Value<string>();
                json = c8o.CallJson("fs://.post",
                    "_id", id,
                    "_rev", rev,
                    "ts", ts,
                    "ts2", ts2
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://.get", "docid", id).Sync();
                Assert.AreEqual(ts, json["ts"].Value<string>());
                Assert.AreEqual(ts2, json["ts2"].Value<string>());
                json = c8o.CallJson("fs://.destroy").Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://.create").Sync();
                Assert.True(json["ok"].Value<bool>());
                try
                {
                    c8o.CallJson("fs://.get", "docid", id).Sync();
                    Assert.True(false, "not possible");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Convertigo.SDK.C8oRessourceNotFoundException", e.GetType().FullName);
                }
            }
        }

        [Test]
        public void C8oFsPostReset()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://.post").Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = json["id"].Value<string>();
                json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                try
                {
                    c8o.CallJson("fs://.get", "docid", id).Sync();
                    Assert.True(false, "not possible");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Convertigo.SDK.C8oRessourceNotFoundException", e.GetType().FullName);
                }
            }
        }

        [Test]
        public void C8oFsPostExisting()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://.post").Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = json["id"].Value<string>();
                try
                {
                    c8o.CallJson("fs://.post", "_id", id).Sync();
                    Assert.True(false, "not possible");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Convertigo.SDK.C8oCouchbaseLiteException", e.GetType().FullName);
                }
            }
        }

        [Test]
        public void C8oFsPostExistingPolicyNone()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://.post", C8o.FS_POLICY, C8o.FS_POLICY_NONE).Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = json["id"].Value<string>();
                try
                {
                    c8o.CallJson("fs://.post",
                        C8o.FS_POLICY, C8o.FS_POLICY_NONE,
                        "_id", id
                    ).Sync();
                    Assert.True(false, "not possible");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Convertigo.SDK.C8oCouchbaseLiteException", e.GetType().FullName);
                }
            }
        }

        [Test]
        public void C8oFsPostExistingPolicyCreate()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var myId = "C8oFsPostExistingPolicyCreate-" + DateTime.Now.Ticks;
                json = c8o.CallJson("fs://.post", "_id", myId).Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = json["id"].Value<string>();
                Assert.AreEqual(myId, id);
                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_CREATE,
                    "_id", id
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                id = json["id"].Value<string>();
                Assert.AreNotSame(myId, id);
            }
        }

        [Test]
        public void C8oFsPostExistingPolicyOverride()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var myId = "C8oFsPostExistingPolicyOverride-" + DateTime.Now.Ticks;
                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_OVERRIDE,
                    "_id", myId,
                    "a", 1,
                    "b", 2
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = json["id"].Value<string>();
                Assert.AreEqual(myId, id);
                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_OVERRIDE,
                    "_id", myId,
                    "a", 3,
                    "c", 4
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                id = json["id"].Value<string>();
                Assert.AreEqual(myId, id);
                json = c8o.CallJson("fs://.get", "docid", myId).Sync();
                Assert.AreEqual(3, json["a"].Value<int>());
                Assert.Null(json["b"]);
                Assert.AreEqual(4, json["c"].Value<int>());
            }
        }

        [Test]
        public void C8oFsPostExistingPolicyMerge()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var myId = "C8oFsPostExistingPolicyMerge-" + DateTime.Now.Ticks;
                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                    "_id", myId,
                    "a", 1,
                    "b", 2
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                var id = json["id"].Value<string>();
                Assert.AreEqual(myId, id);
                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                    "_id", myId,
                    "a", 3,
                    "c", 4
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                id = json["id"].Value<string>();
                Assert.AreEqual(myId, id);
                json = c8o.CallJson("fs://.get", "docid", myId).Sync();
                Assert.AreEqual(3, json["a"].Value<int>());
                Assert.AreEqual(2, json["b"].Value<int>());
                Assert.AreEqual(4, json["c"].Value<int>());
            }
        }

        [Test]
        public void C8oFsPostExistingPolicyMergeSub()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var myId = "C8oFsPostExistingPolicyMergeSub-" + DateTime.Now.Ticks;
                var sub_c = new JObject();
                var sub_f = new JObject();
                sub_c["d"] = 3;
                sub_c["e"] = "four";
                sub_c["f"] = sub_f;
                sub_f["g"] = true;
                sub_f["h"] = new JArray("one", "two", "three", "four");
                json = c8o.CallJson("fs://.post",
                    "_id", myId,
                    "a", 1,
                    "b", -2,
                    "c", sub_c
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                    "_id", myId,
                    "i", new JArray("5", 6, 7.1, null),
                    "c.f.j", "good",
                    "c.f.h", new JArray(true, false)
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                    C8o.FS_SUBKEY_SEPARATOR, "<>",
                    "_id", myId,
                    "c<>i-j", "great"
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://.get", "docid", myId).Sync();
                json.Remove("_rev");
                Assert.AreEqual(myId, json["_id"].Value<string>());
                json.Remove("_id");
                var expectedJson = JObject.Parse(
                    "{\"a\":1,\"c\":{\"i-j\":\"great\",\"f\":{\"h\":[true,false,\"three\",\"four\"],\"j\":\"good\",\"g\":true},\"d\":3,\"e\":\"four\"},\"i\":[\"5\",6,7.1,null],\"b\":-2}"
                );
                assertEquals(expectedJson, json);
            }
        }

        public class PlainObjectA
        {
            public string name;
            public List<PlainObjectB> bObjects;
            public PlainObjectB bObject;
        }

        public class PlainObjectB
        {
            public string name;
            public int num;
            public bool enabled;
        }

        [Test]
        public void C8oFsMergeObject()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var myId = "C8oFsMergeObject-" + DateTime.Now.Ticks;

                var plainObjectA = new PlainObjectA();
                plainObjectA.name = "plain A";
                plainObjectA.bObjects = new List<PlainObjectB>();

                plainObjectA.bObject = new PlainObjectB();
                plainObjectA.bObject.name = "plain B 1";
                plainObjectA.bObject.num = 1;
                plainObjectA.bObject.enabled = true;
                plainObjectA.bObjects.Add(plainObjectA.bObject);

                plainObjectA.bObject = new PlainObjectB();
                plainObjectA.bObject.name = "plain B 2";
                plainObjectA.bObject.num = 2;
                plainObjectA.bObject.enabled = false;
                plainObjectA.bObjects.Add(plainObjectA.bObject);

                plainObjectA.bObject = new PlainObjectB();
                plainObjectA.bObject.name = "plain B -777";
                plainObjectA.bObject.num = -777;
                plainObjectA.bObject.enabled = true;

                json = c8o.CallJson("fs://.post",
                    "_id", myId,
                    "a obj", plainObjectA
                ).Sync();
                Assert.True(json["ok"].Value<bool>());
                plainObjectA.bObjects[1].name = "plain B 2 bis";
                
                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                    "_id", myId,
                    "a obj.bObjects", plainObjectA.bObjects
                ).Sync();
                Assert.True(json["ok"].Value<bool>());

                plainObjectA.bObject = new PlainObjectB();
                plainObjectA.bObject.name = "plain B -666";
                plainObjectA.bObject.num = -666;
                plainObjectA.bObject.enabled = false;

                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                    "_id", myId,
                    "a obj.bObject", plainObjectA.bObject
                ).Sync();
                Assert.True(json["ok"].Value<bool>());

                json = c8o.CallJson("fs://.post",
                    C8o.FS_POLICY, C8o.FS_POLICY_MERGE,
                    "_id", myId,
                    "a obj.bObject.enabled", true
                ).Sync();
                Assert.True(json["ok"].Value<bool>());

                json = c8o.CallJson("fs://.get", "docid", myId).Sync();
                json.Remove("_rev");
                Assert.AreEqual(myId, json["_id"].Value<string>());
                json.Remove("_id");
                var expectedJson = JObject.Parse(
                    "{\"a obj\":{\"name\":\"plain A\",\"bObjects\":[{\"enabled\":true,\"name\":\"plain B 1\",\"num\":1},{\"enabled\":false,\"name\":\"plain B 2 bis\",\"num\":2}],\"bObject\":{\"name\":\"plain B -666\",\"enabled\":true,\"num\":-666}}}"
                 );
                assertEquals(expectedJson, json);
            }
        }

        [Test]
        public void C8oFsPostGetMultibase()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS);
            lock (c8o)
            {
                var json = c8o.CallJson("fs://.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://notdefault.reset").Sync();
                Assert.True(json["ok"].Value<bool>());
                var myId = "C8oFsPostGetMultibase-" + DateTime.Now.Ticks;
                json = c8o.CallJson("fs://.post", "_id", myId).Sync();
                Assert.True(json["ok"].Value<bool>());
                try
                {
                    c8o.CallJson("fs://notdefault.get", "docid", myId).Sync();
                    Assert.True(false, "not possible");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Convertigo.SDK.C8oRessourceNotFoundException", e.GetType().FullName);
                }
                json = c8o.CallJson("fs://notdefault.post", "_id", myId).Sync();
                Assert.True(json["ok"].Value<bool>());
                json = c8o.CallJson("fs://notdefault.get", "docid", myId).Sync();
                var id = json["_id"].Value<string>();
                Assert.AreEqual(myId, id);
            }
        }

        [Test]
        public void C8oFsReplicatePullAnoAndAuth()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PULL);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    try
                    {
                        c8o.CallJson("fs://.get", "docid", "258").Sync();
                        Assert.True(false, "not possible");
                    }
                    catch (Exception e)
                    {
                        Assert.AreEqual("Convertigo.SDK.C8oRessourceNotFoundException", e.GetType().FullName);
                    }
                    json = c8o.CallJson("fs://.replicate_pull").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson("fs://.get", "docid", "258").Sync();
                    var value = json["data"].Value<string>();
                    Assert.AreEqual("258", value);
                    try
                    {
                        c8o.CallJson("fs://.get", "docid", "456").Sync();
                        Assert.True(false, "not possible");
                    }
                    catch (Exception e)
                    {
                        Assert.AreEqual("Convertigo.SDK.C8oRessourceNotFoundException", e.GetType().FullName);
                    }
                    json = c8o.CallJson(".LoginTesting").Sync();
                    value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    json = c8o.CallJson("fs://.replicate_pull").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson("fs://.get", "docid", "456").Sync();
                    value = json["data"].Value<string>();
                    Assert.AreEqual("456", value);
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oFsReplicatePullProgress()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PULL);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson(".LoginTesting").Sync();
                    var value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    int count = 0;
                    string first = null;
                    string last = null;
                    bool uiThread = false;
                    json = c8o.CallJson("fs://.replicate_pull").Progress(progress =>
                    {
                        count++;
                        uiThread |= "FakeUI".Equals(Thread.CurrentThread.Name);
                        if (first == null)
                        {
                            first = progress.ToString();
                        }
                        last = progress.ToString();
                    }).Sync();
                    json = c8o.CallJson("fs://.get", "docid", "456").Sync();
                    value = json["data"].Value<string>();
                    Assert.AreEqual("456", value);
                    Assert.False(uiThread, "uiThread must be False");
                    Assert.AreEqual("pull: 0/0 (running)", first);
                    Assert.AreEqual("pull: 8/8 (done)", last);
                    Assert.True(count > 5, "count > 5");
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oFsReplicatePullProgressUI()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PULL);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson(".LoginTesting").Sync();
                    var value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    int count = 0;
                    string first = null;
                    string last = null;
                    bool uiThread = true;
                    json = c8o.CallJson("fs://.replicate_pull").ProgressUI(progress =>
                    {
                        count++;
                        uiThread &= "FakeUI".Equals(Thread.CurrentThread.Name);
                        if (first == null)
                        {
                            first = progress.ToString();
                        }
                        last = progress.ToString();
                    }).Sync();
                    json = c8o.CallJson("fs://.get", "docid", "456").Sync();
                    value = json["data"].Value<string>();
                    Assert.AreEqual("456", value);
                    Assert.True(uiThread, "uiThread must be True");
                    Assert.AreEqual("pull: 0/0 (running)", first);
                    Assert.AreEqual("pull: 8/8 (done)", last);
                    Assert.True(count > 5, "count > 5");
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oFsReplicatePullAnoAndAuthView()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PULL);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson("fs://.replicate_pull").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson("fs://.view",
                        "ddoc", "design",
                        "view", "reverse"
                    ).Sync();
                    object value = json["rows"][0]["value"].Value<float>();
                    Assert.AreEqual(774.0, value);
                    json = c8o.CallJson("fs://.view",
                        "ddoc", "design",
                        "view", "reverse",
                        "reduce", false
                    ).Sync();
                    value = json["count"].Value<int>();
                    Assert.AreEqual(3, value);
                    value = json["rows"][1]["key"].Value<string>();
                    Assert.AreEqual("852", value);
                    json = c8o.CallJson("fs://.view",
                        "ddoc", "design",
                        "view", "reverse",
                        "startkey", "0",
                        "endkey", "9"
                    ).Sync();
                    value = json["rows"][0]["value"].Value<float>();
                    Assert.AreEqual(405.0, value);
                    json = c8o.CallJson(".LoginTesting").Sync();
                    value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    json = c8o.CallJson("fs://.replicate_pull").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson("fs://.view",
                        "ddoc", "design",
                        "view", "reverse"
                    ).Sync();
                    value = json["rows"][0]["value"].Value<float>();
                    Assert.AreEqual(2142.0, value);
                    json = c8o.CallJson("fs://.view",
                        "ddoc", "design",
                        "view", "reverse",
                        "reduce", false
                    ).Sync();
                    value = json["count"].Value<int>();
                    Assert.AreEqual(6, value);
                    value = json["rows"][1]["key"].Value<string>();
                    Assert.AreEqual("654", value);
                    json = c8o.CallJson("fs://.post", "_id", "111", "data", "16").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson("fs://.view",
                        "ddoc", "design",
                        "view", "reverse",
                        "startkey", "0",
                        "endkey", "9"
                    ).Sync();
                    value = json["rows"][0]["value"].Value<float>();
                    Assert.AreEqual(1000.0, value);
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oFsViewArrayKey()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PULL);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson(".LoginTesting").Sync();
                    object value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    json = c8o.CallJson("fs://.replicate_pull").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson("fs://.view",
                            "ddoc", "design",
                            "view", "array",
                            "startkey", "[\"1\"]"
                    ).Sync();
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oFsReplicatePullGetAll()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PULL);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson(".LoginTesting").Sync();
                    object value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    json = c8o.CallJson("fs://.replicate_pull").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson("fs://.all").Sync();
                    Assert.AreEqual(8, json["count"].Value<int>());
                    Assert.AreEqual(8, json["rows"].Value<JArray>().Count);
                    Assert.AreEqual("789", json["rows"][5]["key"].Value<string>());
                    Assert.Null(json["rows"][5]["doc"]);
                    json = c8o.CallJson("fs://.all",
                        "include_docs", true
                    ).Sync();
                    Assert.AreEqual(8, json["count"].Value<int>());
                    Assert.AreEqual(8, json["rows"].Value<JArray>().Count);
                    Assert.AreEqual("789", json["rows"][5]["key"].Value<string>());
                    Assert.AreEqual("testing_user", json["rows"][5]["doc"]["~c8oAcl"].Value<string>());
                    json = c8o.CallJson("fs://.all",
                        "limit", 2
                    ).Sync();
                    Assert.AreEqual(2, json["count"].Value<int>());
                    Assert.AreEqual(2, json["rows"].Value<JArray>().Count);
                    Assert.AreEqual("147", json["rows"][1]["key"].Value<string>());
                    Assert.Null(json["rows"][1]["doc"]);
                    json = c8o.CallJson("fs://.all",
                        "include_docs", true,
                        "limit", 3,
                        "skip", 2
                    ).Sync();
                    Assert.AreEqual(3, json["count"].Value<int>());
                    Assert.AreEqual(3, json["rows"].Value<JArray>().Count);
                    Assert.AreEqual("369", json["rows"][1]["key"].Value<string>());
                    Assert.AreEqual("doc", json["rows"][1]["doc"]["type"].Value<string>());
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oFsReplicatePushAuth()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PUSH);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    var id = "C8oFsReplicatePushAnoAndAuth-" + DateTime.Now.Ticks;
                    json = c8o.CallJson("fs://.post",
                        "_id", id,
                        "data", "777",
                        "bool", true,
                        "int", 777
                    ).Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson(".LoginTesting").Sync();
                    object value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    json = c8o.CallJson("fs://.replicate_push").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson(".qa_fs_push.GetDocument", "_use_docid", id).Sync();
                    value = json.SelectToken("document.couchdb_output.data").Value<string>();
                    Assert.AreEqual("777", value);
                    value = json.SelectToken("document.couchdb_output.int").Value<int>();
                    Assert.AreEqual(777, value);
                    value = json.SelectToken("document.couchdb_output.~c8oAcl").Value<string>();
                    Assert.AreEqual("testing_user", value);
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oFsReplicatePushAuthProgress()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PUSH);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    var id = "C8oFsReplicatePushAuthProgress-" + DateTime.Now.Ticks;
                    for (int i = 0; i < 10; i++)
                    {
                        json = c8o.CallJson("fs://.post",
                            "_id", id + "-" + i,
                            "index", i
                        ).Sync();
                        Assert.True(json["ok"].Value<bool>());
                    }
                    json = c8o.CallJson(".LoginTesting").Sync();
                    object value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    int count = 0;
                    string first = null;
                    string last = null;
                    bool uiThread = false;
                    json = c8o.CallJson("fs://.replicate_push").Progress(progress =>
                    {
                        count++;
                        uiThread |= "FakeUI".Equals(Thread.CurrentThread.Name);
                        if (first == null)
                        {
                            first = progress.ToString();
                        }
                        last = progress.ToString();
                    }).Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson(".qa_fs_push.AllDocs",
                        "startkey", id,
                        "endkey", id + "z"
                    ).Sync();
                    var array = json.SelectToken("document.couchdb_output.rows").Value<JArray>();
                    Assert.AreEqual(10, array.Count);
                    for (int i = 0; i < 10; i++)
                    {
                        value = array[i].SelectToken("doc._id").Value<string>();
                        Assert.AreEqual(id + "-" + i, value);
                        value = array[i].SelectToken("doc.index").Value<int>();
                        Assert.AreEqual(i, value);
                        value = array[i].SelectToken("doc.~c8oAcl").Value<string>();
                        Assert.AreEqual("testing_user", value);
                    }
                    Assert.False(uiThread, "uiThread must be False");
                    Assert.AreEqual("push: 0/0 (running)", first);
                    Assert.AreEqual("push: 10/10 (done)", last);
                    Assert.True(count > 3, "count > 3");
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oFsReplicateSyncContinuousProgress()
        {
            var c8o = Get<C8o>(Stuff.C8O_FS_PUSH);
            lock (c8o)
            {
                try
                {
                    var json = c8o.CallJson("fs://.reset").Sync();
                    Assert.True(json["ok"].Value<bool>());
                    var id = "C8oFsReplicateSyncContinuousProgress-" + DateTime.Now.Ticks;
                    for (int i = 0; i < 3; i++)
                    {
                        json = c8o.CallJson("fs://.post",
                            "_id", id + "-" + i,
                            "index", i
                        ).Sync();
                        Assert.True(json["ok"].Value<bool>());
                    }
                    json = c8o.CallJson(".LoginTesting").Sync();
                    object value = json.SelectToken("document.authenticatedUserID").Value<string>();
                    Assert.AreEqual("testing_user", value);
                    string firstPush = null;
                    string lastPush = null;
                    string livePush = null;
                    string firstPull = null;
                    string lastPull = null;
                    string livePull = null;
                    json = c8o.CallJson("fs://.sync", "continuous", true).Progress(progress =>
                    {
                        if (progress.Continuous)
                        {
                            if (progress.Push)
                            {
                                livePush = progress.ToString();
                            }
                            if (progress.Pull)
                            {
                                livePull = progress.ToString();
                            }
                        }
                        else
                        {
                            if (progress.Push)
                            {
                                if (firstPush == null)
                                {
                                    firstPush = progress.ToString();
                                }
                                lastPush = progress.ToString();
                            }
                            if (progress.Pull)
                            {
                                if (firstPull == null)
                                {
                                    firstPull = progress.ToString();
                                }
                                lastPull = progress.ToString();
                            }
                        }
                    }).Sync();
                    Assert.True(json["ok"].Value<bool>());
                    Assert.AreEqual("push: 0/0 (running)", firstPush);
                    Assert.True(Regex.IsMatch(lastPush, "push: \\d+/\\d+ \\(done\\)"), "push: \\d+/\\d+ \\(done\\) for " + lastPush);
                    Assert.AreEqual("pull: 0/0 (running)", firstPull);
                    Assert.True(Regex.IsMatch(lastPull, "pull: \\d+/\\d+ \\(done\\)"), "pull: \\d+/\\d+ \\(done\\) for " + lastPull);
                    json = c8o.CallJson(".qa_fs_push.AllDocs",
                        "startkey", id,
                        "endkey", id + "z"
                    ).Sync();
                    var array = json.SelectToken("document.couchdb_output.rows").Value<JArray>();
                    Assert.AreEqual(3, array.Count);
                    for (int i = 0; i < 3; i++)
                    {
                        value = array[i].SelectToken("doc._id").Value<string>();
                        Assert.AreEqual(id + "-" + i, value);
                    }
                    json = c8o.CallJson("fs://.get", "docid", "def").Sync();
                    value = json["_id"].Value<string>();
                    Assert.AreEqual("def", value);
                    json["custom"] = id;
                    json = c8o.CallJson("fs://.post", json).Sync();
                    Assert.True(json["ok"].Value<bool>());
                    json = c8o.CallJson(".qa_fs_push.PostDocument", "_id", "ghi", "custom", id).Sync();
                    Assert.True(json.SelectToken("document.couchdb_output.ok").Value<bool>());
                    Thread.Sleep(2000);
                    json = c8o.CallJson("fs://.get", "docid", "ghi").Sync();
                    value = json["custom"].Value<string>();
                    Assert.AreEqual(id, value);
                    json = c8o.CallJson(".qa_fs_push.GetDocument", "_use_docid", "def").Sync();
                    value = json.SelectToken("document.couchdb_output.custom").Value<string>();
                    Assert.AreEqual(id, value);
                    Assert.True(Regex.IsMatch(livePull, "pull: \\d+/\\d+ \\(live\\)"), "pull: \\d+/\\d+ \\(live\\) for " + livePull);
                    Assert.True(Regex.IsMatch(livePush, "push: \\d+/\\d+ \\(live\\)"), "push: \\d+/\\d+ \\(live\\) for " + livePush);
                }
                finally
                {
                    c8o.CallJson(".LogoutTesting").Sync();
                }
            }
        }

        [Test]
        public void C8oLocalCacheXmlPriorityLocal()
        {
            var c8o = Get<C8o>(Stuff.C8O_LC);
            var id = "C8oLocalCacheXmlPriorityLocal-" + DateTime.Now.Ticks;
            var doc = c8o.CallXml(".Ping",
                C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 3000),
                "var1", id
            ).Sync();
            var value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual(id, value);
            var signature = doc.XPathSelectElement("/document").Attribute("signature").Value;
            Thread.Sleep(100);
            doc = c8o.CallXml(".Ping",
                C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 3000),
                "var1", id + "bis"
            ).Sync();
            value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual(id + "bis", value);
            var signature2 = doc.XPathSelectElement("/document").Attribute("signature").Value;
            Assert.AreNotEqual(signature, signature2);
            Thread.Sleep(100);
            doc = c8o.CallXml(".Ping",
                 C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 3000),
                 "var1", id
            ).Sync();
            value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual(id, value);
            signature2 = doc.XPathSelectElement("/document").Attribute("signature").Value;
            Assert.AreEqual(signature, signature2);
            Thread.Sleep(2800);
            doc = c8o.CallXml(".Ping",
                 C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 3000),
                 "var1", id
            ).Sync();
            value = doc.XPathSelectElement("/document/pong/var1").Value;
            Assert.AreEqual(id, value);
            signature2 = doc.XPathSelectElement("/document").Attribute("signature").Value;
            Assert.AreNotEqual(signature, signature2);
        }

        [Test]
        public void C8oLocalCacheJsonPriorityLocal()
        {
            var c8o = Get<C8o>(Stuff.C8O_LC);
            var id = "C8oLocalCacheJsonPriorityLocal-" + DateTime.Now.Ticks;
            var json = c8o.CallJson(".Ping",
                C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 3000),
                "var1", id
            ).Sync();
            var value = json.SelectToken("document.pong.var1").Value<string>();
            Assert.AreEqual(id, value);
            var signature = json.SelectToken("document.attr.signature").Value<string>();
            Thread.Sleep(100);
            json = c8o.CallJson(".Ping",
                C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 3000),
                "var1", id + "bis"
            ).Sync();
            value = json.SelectToken("document.pong.var1").Value<string>();
            Assert.AreEqual(id + "bis", value);
            var signature2 = json.SelectToken("document.attr.signature").Value<string>();
            Assert.AreNotEqual(signature, signature2);
            Thread.Sleep(100);
            json = c8o.CallJson(".Ping",
                 C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 3000),
                 "var1", id
            ).Sync();
            value = json.SelectToken("document.pong.var1").Value<string>();
            Assert.AreEqual(id, value);
            signature2 = json.SelectToken("document.attr.signature").Value<string>();
            Assert.AreEqual(signature, signature2);
            Thread.Sleep(2800);
            json = c8o.CallJson(".Ping",
                 C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 3000),
                 "var1", id
            ).Sync();
            value = json.SelectToken("document.pong.var1").Value<string>();
            Assert.AreEqual(id, value);
            signature2 = json.SelectToken("document.attr.signature").Value<string>();
            Assert.AreNotEqual(signature, signature2);
        }

        [Test]
        public void C8oFileTransferDownloadSimple()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            lock (c8o)
            {
                C8oFileTransfer ft = new C8oFileTransfer(c8o, new C8oFileTransferSettings());
                c8o.CallJson(ft.TaskDb + ".destroy").Sync();
                var status = new C8oFileTransferStatus[] { null };
                var error = new Exception[] { null };
                ft.RaiseTransferStatus += (sender, statusEvent) =>
                {
                    if (statusEvent.State == C8oFileTransferStatus.StateFinished)
                    {
                        lock (status)
                        {
                            status[0] = statusEvent;
                            Monitor.Pulse(status);
                        }
                    }
                };
                ft.RaiseException += (sender, errorEvent) =>
                {
                    lock (status)
                    {
                        error[0] = errorEvent;
                        Monitor.Pulse(status);
                    }
                };
                ft.Start();
                var uuid = c8o.CallXml(".PrepareDownload4M").Sync().XPathSelectElement("/document/uuid").Value;
                Assert.NotNull(uuid);
                var filepath = Path.GetTempFileName();
                var fileInfo = new FileInfo(filepath);
                try
                {
                    lock (status)
                    {
                        ft.DownloadFile(uuid, filepath).ConfigureAwait(false).GetAwaiter().GetResult();
                        Monitor.Wait(status, 20000);
                    }
                    if (error[0] != null)
                    {
                        throw error[0];
                    }
                    Assert.NotNull(status[0]);
                    Assert.True(fileInfo.Exists);
                    var length = fileInfo.Length;
                    Assert.AreEqual(4237409, length);

                }
                finally
                {
                    fileInfo.Delete();
                }
            }
        }

        [Test]
        public void C8oFileTransferUploadSimple()
        {
            var c8o = Get<C8o>(Stuff.C8O);
            lock (c8o)
            {
                Couchbase.Lite.Storage.ForestDB.Plugin.Register();
                c8o.FullSyncStorageEngine = C8o.FS_STORAGE_FORESTDB;
                C8oFileTransfer ft = new C8oFileTransfer(c8o, new C8oFileTransferSettings());
                c8o.CallJson(ft.TaskDb + ".destroy").Sync();
                var status = new C8oFileTransferStatus[] { null };
                var error = new Exception[] { null };
                ft.RaiseTransferStatus += (sender, statusEvent) =>
                {
                    if (statusEvent.State == C8oFileTransferStatus.StateFinished)
                    {
                        lock (status)
                        {
                            status[0] = statusEvent;
                            Monitor.Pulse(status);
                        }
                    }
                };
                ft.RaiseException += (sender, errorEvent) =>
                {
                    lock (status)
                    {
                        error[0] = errorEvent;
                        Monitor.Pulse(status);
                    }
                };
                ft.Start();
                lock (status)
                {
                    var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var stream = new FileStream(path + @"\Resources\4m.jpg", FileMode.Open);
                    ft.UploadFile("4m.jpg", stream).ConfigureAwait(false).GetAwaiter().GetResult();
                    Monitor.Wait(status, 20000);
                }
                if (error[0] != null)
                {
                    throw error[0];
                }
                Assert.NotNull(status[0]);
                var filepath = status[0].ServerFilepath;
                var length = c8o.CallXml(".GetSizeAndDelete", "filepath", filepath).Sync().XPathSelectElement("/document/length").Value;
                Assert.AreEqual("4237409", length);
            }
        }

        private void assertEqualsJsonChild(JToken expectedToken, JToken actualToken)
        {
            var expectedObject = expectedToken.Value<object>();
            var actualObject = actualToken.Value<object>();
            if (expectedObject != null)
            {
                Assert.NotNull(actualObject, "must not be null");
                Assert.AreEqual(expectedObject.GetType(), actualObject.GetType());
                if (expectedObject is JObject) {
                    assertEquals(expectedObject as JObject, actualObject as JObject);
                } else if (expectedObject is JArray) {
                    assertEquals(expectedObject as JArray, actualObject as JArray);
                } else {
                    Assert.AreEqual(expectedObject, actualObject);
                }
            }
            else
            {
                Assert.Null(actualObject, "must be null");
            }
        }

        private void assertEquals(JObject expected, JObject actual)
        {
            Assert.AreEqual(expected.Count, actual.Count, "missing keys: " + expected.Count + " and " + actual.Count);

            foreach (var entry in expected)
            {
                String expectedName = entry.Key;
                Assert.NotNull(actual[expectedName], "missing key: " + expectedName);
                assertEqualsJsonChild(expected[expectedName], actual[expectedName]);
            }
        }

        private void assertEquals(JArray expected, JArray actual)
        {
            Assert.AreEqual(expected.Count, actual.Count, "missing entries");

            for (int i = 0; i < expected.Count; i++)
            {
                assertEqualsJsonChild(expected[i], actual[i]);
            }
        }
    }
}
