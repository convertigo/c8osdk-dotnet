using Convertigo.SDK;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
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
            internal static readonly Stuff C8O = new Stuff(() =>
            {
                return new C8o("http://" + HOST + ":28080" + PROJECT_PATH);
            });

            private Stuff(Func<object> get)
            {
                this.get = get;
            }

            Func<object> get;

            internal Func<object> Get
            {
                get { return get; }
            }
        }

        IDictionary<Stuff, object> stuffs = new Dictionary<Stuff, object>();

        T Get<T>(Stuff stuff)
        {
            lock (stuff)
            {
                object res = stuffs.ContainsKey(stuff) ? stuffs[stuff] : null;
                if (res == null)
                {
                    try
                    {
                        res = stuff.Get();
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
        public void C8oDefaultPingTwoSingleValue()
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
    }
}
