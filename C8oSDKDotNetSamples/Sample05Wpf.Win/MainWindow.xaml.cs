using Convertigo.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sample05Wpf.Win
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        C8o c8o;

        public MainWindow()
        {
            InitializeComponent();

            c8o = new C8o("http://tonus.twinsoft.fr:18080/convertigo/projects/Sample05",
                new C8oSettings().SetUiDispatcher(code =>
                {
                    Dispatcher.BeginInvoke(code);
                }).SetFullSyncUsername("admin")
                .SetFullSyncPassword("admin")
                .SetDefaultDatabaseName("sample05")
            );
        }

        private async void OnTest01(object sender, EventArgs args)
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

        private async void OnTest02(object sender, EventArgs args)
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

        private void OnTest03(object sender, EventArgs args)
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

        private void OnTest04(object sender, EventArgs args)
        {
            Output.Text = "Test04\n";
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
    }
}
