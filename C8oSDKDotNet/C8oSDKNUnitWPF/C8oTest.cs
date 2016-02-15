using Convertigo.SDK;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
                return new C8o("http://" + HOST + ":28080" + PROJECT_PATH);
            });

            internal static readonly Stuff C8O_BIS = new Stuff(() =>
            {
                return new C8o("http://" + HOST + ":28080" + PROJECT_PATH);
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
                        throw (Exception)res;
                    }

                    T t = (T)res;

                    return t;
                }
            }
        }

        T Get<T>(Stuff stuff)
        {
            return Stuff.Get<T>(stuff);
        }

        [SetUp]
        public void SetUp()
        {
            // before any tests
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
    }
}
