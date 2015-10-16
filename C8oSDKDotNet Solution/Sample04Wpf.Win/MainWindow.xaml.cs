using C8oBigFileTransfer;
using Convertigo.SDK;
using Convertigo.SDK.Listeners;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Sample04Wpf.Win
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal C8o c8o;
        internal BigFileTransferInterface bigFileTransfer;

        private C8oJsonResponseListener jsonListener;

        public MainWindow()
        {
            InitializeComponent();

            ContentArea.Content = new Login(this);

            jsonListener = new C8oJsonResponseListener((jsonResponse, parameters) =>
            {
                Dispatcher.Invoke(() =>
                {
                    OutputArea.Text = jsonResponse.ToString();
                });
            });

            String c8oEndpoint = "http://tonus.twinsoft.fr:18080/convertigo/projects/";

            c8o = new C8o(c8oEndpoint + "BigFileTransferSample");

            bigFileTransfer = new BigFileTransferInterface(c8oEndpoint + "lib_BigFileTransfer", new C8oSettings()
                .SetDefaultFullSyncDatabaseName("bigfiletransfer")
                .SetFullSyncUsername("admin")
                .SetFullSyncPassword("admin")
            , new FileManager((path) =>
            {
                FileStream fileStream = File.Create(path);
                return fileStream;
            }, (path) =>
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                return fileStream;
            }));
        }
    }
}
