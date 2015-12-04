﻿using Convertigo.SDK;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sample05Shared
{
    class Sample05
    {
        C8o c8o;
        CrossOuput Output;
        CrossDebug Debug;

        public Sample05(Action<Action> uiDispatcher, Action<string> output, Action<string> debug)
        {
            Output = new CrossOuput(output);
            Debug = new CrossDebug(debug);

            C8oPlatform.Init();

            c8o = new C8o("http://tonus.twinsoft.fr:18080/convertigo/projects/Sample05",
                new C8oSettings().SetUiDispatcher(uiDispatcher).SetFullSyncUsername("admin")
                .SetFullSyncPassword("admin")
                .SetDefaultDatabaseName("sample05")
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
                Debug.WriteLine("" + progress);
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
