using Convertigo.SDK;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample05Shared
{
    class Sample05
    {
        static public byte[] cert;
        C8o c8o;
        CrossOuput Output;
        CrossDebug Debug;

        public Sample05(Action<string> output, Action<string> debug)
        {
            C8oPlatform.Init();

            Output = new CrossOuput(output);
            Debug = new CrossDebug(debug);

            c8o = new C8o("http://192.168.100.95:18080/convertigo/projects/Sample05",
            // c8o = new C8o("http://pulse.twinsoft.fr:18080/convertigo/projects/Sample05",
            // c8o = new C8o("http://trial.convertigo.net/cems/projects/Sample05",
                new C8oSettings().SetFullSyncUsername("admin")
                .SetFullSyncPassword("admin")
                .SetDefaultDatabaseName("sample05")
                .SetLogLevelLocal(C8oLogLevel.TRACE)
            );
        }

        public async Task OnTest01(object sender, EventArgs args)
        {
            Output.Text = "Test01\n";
            Output.Text += "==========\n";
            var xml = await c8o.CallXml(".sample05.GetServerInfo").Async();
            Output.Text += xml.ToString();
            Output.Text += "\n==========\n";
            c8o.CallJson(".sample05.GetServerInfo").ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n==========\n";
                return null;
            });
        }

        public async Task OnTest02(object sender, EventArgs args)
        {
            Output.Text = "Test02\n";
            Output.Text += "==========\n";
            var xml = await c8o.CallXml(".Ping",
                "pong1", "PoNG",
                "pong2", "PoooonG"
            ).Async();
            Output.Text += xml.ToString();
            Output.Text += "\n==========\n";
            var json = await c8o.CallJson(".Ping",
                "pong1", "PoNG",
                "pong2", "PoooonG"
            ).Async();
            Output.Text += json.ToString();
            Output.Text += "\n==========\n";
            json = await c8o.CallJson(".Ping", new JObject()
            {
                { "pong1", "with" },
                { "pong2", "JObject" }
            }).Async();
            Output.Text += json.ToString();
            Output.Text += "\n==========\n";
        }

        public void OnTest03(object sender, EventArgs args)
        {
            Output.Text = "Test03\n";
            Output.Text += "========== create ==\n";
            c8o.CallJson("fs://.create").ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== post ==\n";
                return c8o.CallJson("fs://.post",
                    "test", "ok"
                );
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== get ==\n";
                string id = json.SelectToken("id").ToString();
                return c8o.CallJson("fs://.get", "docid", id);
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== delete ==\n";
                string id = json.SelectToken("_id").ToString();
                return c8o.CallJson("fs://.delete", "docid", id);
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n==========\n";
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest04(object sender, EventArgs args)
        {
            long cpt = 0;
            Output.Text = "Test04\n";
            Output.Text += "========== auth ==\n";
            c8o.CallJson(".Auth", "user", "Test04").ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== destroy ==\n";
                return c8o.CallJson("fs://.destroy");
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== post ==\n";
                return c8o.CallJson("fs://.post", "_id", "00", "good", true);
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== all ==\n";
                return c8o.CallJson("fs://.all");
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json["total_rows"].ToString();
                Output.Text += "\n========== sync ==\n";
                return c8o.CallJson("fs://.sync", "continuous", true);
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== all ==\n";
                return c8o.CallJson("fs://.all");
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json["total_rows"].ToString();
                Output.Text += "\n==========\n";
                return null;
            }).Progress(progress =>
            {
                Debug.WriteLine("" + cpt++ + " " + progress);
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest05(object sender, EventArgs args)
        {
            Output.Text = "Test05\n";
            Output.Text += "========== get 1 ==\n";
            c8o.CallJson("fs://.get", "docid", "1").ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest06(object sender, EventArgs args)
        {
            Output.Text = "Test06\n";

            var c8o = new C8o("https://tonus.twinsoft.fr:18081/convertigo/projects/Sample05", new C8oSettings().SetTrustAllCertificates(false));

            c8o.CallXml(".sample05.GetServerInfo").ThenUI((xml, param) =>
            {
                Output.Text += xml.ToString();
                Output.Text += "\n==========\n";
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest07(object sender, EventArgs args)
        {
            Output.Text = "Test07\n";

            var c8o = new C8o("https://tonus.twinsoft.fr:18081/convertigo/projects/Sample05", new C8oSettings().SetTrustAllCertificates(true));

            c8o.CallXml(".sample05.GetServerInfo").ThenUI((xml, param) =>
            {
                Output.Text += xml.ToString();
                Output.Text += "\n==========\n";
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest08(object sender, EventArgs args)
        {
            Output.Text = "Test08\n";

            var c8o = new C8o("https://tonus.twinsoft.fr:28443/convertigo/projects/Sample05", new C8oSettings()
                .SetDefaultDatabaseName("sample05")
                .SetTrustAllCertificates(true)
                .AddClientCertificate(cert, "password")
            );

            int cpt = 0;
            Output.Text += "========== auth ==\n";
            c8o.CallJson(".Auth", "user", "Test08").ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== destroy ==\n";
                return c8o.CallJson("fs://.destroy");
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== post ==\n";
                return c8o.CallJson("fs://.post", "_id", "00", "good", true);
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== all ==\n";
                return c8o.CallJson("fs://.all");
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json["total_rows"].ToString();
                Output.Text += "\n========== sync ==\n";
                return c8o.CallJson("fs://.sync", "continuous", true);
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n========== all ==\n";
                return c8o.CallJson("fs://.all");
            }).ThenUI((json, parameters) =>
            {
                Output.Text += json["total_rows"].ToString();
                Output.Text += "\n==========\n";
                return null;
            }).Progress(progress =>
            {
                Debug.WriteLine("" + cpt++ + " " + progress);
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest09(object sender, EventArgs args)
        {
            Output.Text = "Test09\n";

            c8o.CallXml("fs://.view",
                "ddoc", "design",
                "view", "modulos",
                "limit", 15,
                "reduce", false
            ).ThenUI((xml, param) =>
            {
                Output.Text += xml.ToString();
                Output.Text += "\n==========\n";
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest10(object sender, EventArgs args)
        {
            Output.Text = "Test10\n";

            c8o.CallXml("fs://.view",
                "ddoc", "design",
                "view", "modulos",
                "reduce", true,
                "group", true
            ).ThenUI((xml, param) =>
            {
                Output.Text += xml.ToString();
                Output.Text += "\n==========\n";
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest11(object sender, EventArgs args)
        {
            Output.Text = "Test11\n";

            var obj = new JObject()
            {
                { "_id", "jobject" },
                { "sub", new JObject() { { "ok", true } } },
                { "bad", false }
            };

            c8o.CallJson("fs://.delete", "docid", "jobject").ThenUI((json, param) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n==========\n";

                return c8o.CallJson("fs://.post", obj);
            }).ThenUI((json, param) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n==========\n";

                return c8o.CallJson("fs://.get", "docid", "jobject");
            }).ThenUI((json, param) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n==========\n";

                json["bad"] = "no way";
                json["new"] = true;
                json["sub"] = new JObject() { { "bis", 3.14 } };
                json[C8o.FS_POLICY] = "merge";

                return c8o.CallJson("fs://.post", json);
            }).ThenUI((json, param) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n==========\n";

                return c8o.CallJson("fs://.get", "docid", "jobject");
            }).ThenUI((json, param) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n==========\n";

                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest12(object sender, EventArgs args)
        {
            Output.Text = "Test12\n";
            Output.Text += "=== Ping local cache priority local 10s ==\n";

            c8o.CallJson(".Ping",
                "pong1", "local cache?",
                "pong2", "I hope priority local",
                C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 10000)
            ).ThenUI((json, param) =>
            {
                Output.Text += json.ToString();
                Output.Text += "\n==========\n";
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest13(object sender, EventArgs args)
        {
            Output.Text = "Test13\n";
            Output.Text += "=== Ping local cache priority server 30s ==\n";

            c8o.CallXml(".Ping",
                "pong1", "local cache?",
                "pong2", "I hope priority server",
                C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.SERVER, 30000)
            ).ThenUI((xml, param) =>
            {
                Output.Text += xml.ToString();
                Output.Text += "\n==========\n";
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }

        public void OnTest14(object sender, EventArgs args)
        {
            Output.Text = "Test14\n";

            //string[] multi2 = new string[] { "m1", "m2", "m3" };
            List<string> multi2 = new List<string> { "m1", "m2", "m3" };

            c8o.CallXml(".MultiVar",
                "simple1", "simple value",
                "simple2", multi2,
                "multi1", "multi1 value",
                "multi2", multi2).ThenUI((xml, param) =>
            {
                Output.Text += xml.ToString();
                Output.Text += "\n==========\n";
                return null;
            }).FailUI((e, parameters) =>
            {
                Output.Text += "\n========== fail ==\n" + e;
            });
        }
    }

    class CrossOuput
    {
        Action<string> output;
        public string text;

        public CrossOuput(Action<string> output)
        {
            this.output = output;
        }

        public string Text
        {
            get
            {
                return text;
            }

            set
            {
                text = value;
                output(text);
            }
        }
    }

    class CrossDebug
    {
        public readonly Action<string> WriteLine;

        public CrossDebug(Action<string> debug)
        {
            WriteLine = debug;
        }
    }
}
