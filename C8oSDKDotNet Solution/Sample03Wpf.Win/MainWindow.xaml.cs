using Convertigo.SDK;
using Convertigo.SDK.FullSync.Enums;
using Convertigo.SDK.Listeners;
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

            String endpoint = "http://devus.twinsoft.fr:18080/convertigo/projects/couchDB";
            C8oSettings c8oSettings = new C8oSettings();
            c8oSettings.fullSyncInterface = new FullSyncHttp("http://localhost:5984", "admin", "admin");
            c8oSettings.defaultFullSyncDatabaseName = "fsdebug_fullsync";

            c8o = new C8o(endpoint, c8oSettings);
        }

        private void CallButtonClick(object sender, RoutedEventArgs e)
        {
            c8o.Call(".CouchDB.GetServerInfo", new Dictionary<string,object>(), jsonListener);
        }

        private void CallButtonFsDocClick(object sender, RoutedEventArgs e)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            data.Add(FullSyncGetDocumentParameter.DOCID.name, "fix");

            c8o.Call("fs://.get", data, jsonListener);
        }

        private void CallButtonFsDeleteDocClick(object sender, RoutedEventArgs e)
        {
            
            Dictionary<string, object> data = new Dictionary<string, object>();

            data.Add(FullSyncDeleteDocumentParameter.DOCID.name, "del");

            c8o.Call("fs://.delete", data, jsonListener);
        }

        private void CallButtonFsPostDocClick(object sender, RoutedEventArgs e)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            data.Add("_id", "post");
            data.Add("data", "ok");
            data.Add(FullSyncPostDocumentParameter.POLICY.name, FullSyncPolicy.MERGE.value);
            data.Add("sub.data2", "good");
            data.Add("sub.data4", "great!");

            c8o.Call("fs://.post", data, jsonListener);
        }

        private void CallButtonFsAllDocsClick(object sender, RoutedEventArgs e)
        {
            c8o.Call("fs://.all", new Dictionary<String, Object>(), jsonListener);
        }

        private void CallButtonFsViewClick(object sender, RoutedEventArgs e)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            data.Add(FullSyncGetViewParameter.DDOC.name, "ddoc");
            data.Add(FullSyncGetViewParameter.VIEW.name, "ifdata");
            // data.Add("key", "\"fix\"");

            c8o.Call("fs://.view", data, jsonListener);
        }
    }
}
