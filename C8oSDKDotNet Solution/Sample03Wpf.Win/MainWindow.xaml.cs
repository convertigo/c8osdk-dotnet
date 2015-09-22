using Convertigo.SDK;
using Convertigo.SDK.FullSync.Enums;
using Convertigo.SDK.Listeners;
using Newtonsoft.Json.Linq;
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

namespace Sample03Wpf.Win
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private C8o c8o;
        private C8oJsonResponseListener jsonListener;

        public MainWindow()
        {
            InitializeComponent();

            jsonListener = new C8oJsonResponseListener((jsonResponse, parameters) =>
            {
                Dispatcher.Invoke(() =>
                {
                    OutputArea.Text = jsonResponse.ToString();
                });
            });
            
            c8o = new C8o("http://tonus.twinsoft.fr:18080/convertigo/projects/FsDebug", new C8oSettings()
                .SetDefaultFullSyncDatabaseName("fsdebug_fullsync")
                .SetFullSyncUsername("admin")
                .SetFullSyncPassword("admin")
            );
        }

        private void CallButtonClick(object sender, RoutedEventArgs e)
        {
            //c8o.Call("..GetServerInfo", new Dictionary<string,object>(), jsonListener);
            c8o.Call(".Set42", new Dictionary<string, object>(), jsonListener);
        }

        private void CallButtonFsDocClick(object sender, RoutedEventArgs e)
        {
            c8o.Call("fs://.get", new Dictionary<string, object>
            {
                {FullSyncGetDocumentParameter.DOCID.name, "fix"}
            }, jsonListener);
        }

        private void CallButtonFsDeleteDocClick(object sender, RoutedEventArgs e)
        {
            c8o.Call("fs://.delete", new Dictionary<string, object>
            {
                {FullSyncDeleteDocumentParameter.DOCID.name, "del"}
            }, jsonListener);
        }

        private void CallButtonFsPostDocClick(object sender, RoutedEventArgs e)
        {
            c8o.Call("fs://.post", new Dictionary<string, object>
            {
                {"_id", "post"},
                {"data", "ok"},
                {FullSyncPostDocumentParameter.POLICY.name, FullSyncPolicy.MERGE.value},
                {"sub.data2", "good"},
                {"sub.data4", "great!"}
            }, jsonListener);
        }

        private void CallButtonFsAllDocsClick(object sender, RoutedEventArgs e)
        {
            c8o.Call("fs://.all", new Dictionary<String, Object>(), jsonListener);
        }

        private void CallButtonFsViewClick(object sender, RoutedEventArgs e)
        {
            c8o.Call("fs://.view", new Dictionary<String, Object>
            {
                {FullSyncGetViewParameter.DDOC.name, "ddoc"},
                {FullSyncGetViewParameter.VIEW.name, "ifdata"}
            }, jsonListener);
        }

        private void CallButtonFsPullClick(object sender, RoutedEventArgs e)
        {
            Dictionary<String, Object> data = new Dictionary<String, Object>();

            c8o.Call("fs://retaildb.replicate_pull", data, jsonListener);
        }

        private void CallButtonFsPushClick(object sender, RoutedEventArgs e)
        {
            Dictionary<String, Object> data = new Dictionary<String, Object>();

            c8o.Call("fs://retaildb.replicate_push", data, jsonListener);
        }

        private void CallButtonFsSyncClick(object sender, RoutedEventArgs e)
        {

            Dictionary<String, Object> data = new Dictionary<String, Object>();

            c8o.Call("fs://retaildb.sync", data, jsonListener);
        }

        private void CallButtonFsResetClick(object sender, RoutedEventArgs e)
        {
            Dictionary<String, Object> data = new Dictionary<String, Object>();

            c8o.Call("fs://retaildb.reset", data, jsonListener);
        }

        async private void CallButtonIncClick(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject res = await c8o.CallJson(".Inc", new Dictionary<string, object> { { "input", "10" } });
                res = await c8o.CallJson(".Inc", new Dictionary<string, object> { { "input", res.SelectToken("document.output") } });
                res = await c8o.CallJson(".Inc", new Dictionary<string, object> { { "input", res.SelectToken("document.output") } });
                jsonListener.OnJsonResponse(res, new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                OutputArea.Text = "Exception: " + ex;
            }
        }
    }
}
