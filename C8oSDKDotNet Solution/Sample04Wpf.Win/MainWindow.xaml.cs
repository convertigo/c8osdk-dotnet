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
        private C8o c8o;
        private C8oJsonResponseListener jsonListener;
        private BigFileTransferInterface bigFileTransfer;

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

            c8o = new C8o("http://tonus.twinsoft.fr:18080/convertigo/projects/BigFileTransfer", new C8oSettings()
                .SetDefaultFullSyncDatabaseName("bigfiletransfer")
                .SetFullSyncUsername("admin")
                .SetFullSyncPassword("admin")
            );

            bigFileTransfer = new BigFileTransferInterface(c8o, "bigfiletransfer", new FileManager((path) =>
            {
                FileStream fileStream = File.Create(path);
                return fileStream;
            }, (path) =>
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                return fileStream;
            }));
        }

        private void Dl200mClick(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => bigFileTransfer.DownloadFile("video200M.mp4", "D:\\TMP\\200m.mp4", (String str) =>
            {

                Dispatcher.Invoke(() =>
                {
                    OutputArea.Text = str;
                });

            }, (DownloadStatus status) =>
            {

            }));
        }

        private void Dl40kClick(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => bigFileTransfer.DownloadFile("image40K.png", "D:\\TMP\\40k.png", (String str) =>
            {
               
                Dispatcher.Invoke(() =>
                {
                    OutputArea.Text = str;
                });
                
            }, (DownloadStatus status) =>
            {

            }));
        }

        private void AllDocsClick(object sender, RoutedEventArgs e)
        {
            c8o.Call("fs://.all", null, jsonListener);
        }
    }
}
