using BigFileTransfer;
using Convertigo.SDK;
using Convertigo.SDK.FullSync;
using Convertigo.SDK.Listeners;
using Convertigo.SDK.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BigFileTransferSampleForms
{
    public partial class MainPage : ContentPage
    {

        private BigFileTransferInterface bfti;
        private C8o c8o;
        private String databaseName;

        public MainPage(FullSyncInterface fullSyncInterface, FileManager fileManager)
        {
            InitializeComponent();

            // Initializes c8o variables
            C8oSettings c8oSettings = new C8oSettings();
            // c8oSettings.fullSyncInterface = fullSyncInterface;
            C8oExceptionListener c8oExceptionListener = new C8oExceptionListener((exception, requestParameters) => 
            {
                String errMsg = "";
                while (exception != null)
                {
                    errMsg = errMsg + exception.Message;
                    exception = exception.InnerException;
                }
                Debug.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!   " + errMsg);
                Task task = new Task(async () =>
                {
                    try
                    {
                        Action<String> progress = new Action<String>((data) =>
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                this.infoLabel.Text = errMsg;
                            });
                        });
                    }
                    catch (Exception e)
                    {
                        String t = "";
                    }
                });

                task.Start();

            });
            this.c8o = new C8o("http://192.168.100.86:18080/convertigo/projects/TestClientSDK", c8oSettings, c8oExceptionListener);
            this.databaseName = "bigfiletransfer";
            this.bfti = new BigFileTransferInterface(c8o, this.databaseName, fileManager);
        }

        void OnStoreButtonClicked(object sender, EventArgs args)
        {
            String fileId = fileIdEntry.Text;
            String destinationPath = destinationPathEntry.Text;

            fileIdEntry.AddToHistory(fileId);
            destinationPathEntry.AddToHistory(destinationPath);

            Task task = new Task(async () =>
            {
                try
                {
                    Action<String> progress = new Action<String>((data) =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            infoLabel.Text = data;
                        });
                    });

                    Action<DownloadStatus> progressBis = new Action<DownloadStatus>((status) =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            statusLabel.Text = status.Current + " / " + status.Total;
                        });
                    });

                    await this.bfti.DownloadFile(fileId, destinationPath, progress, progressBis);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        infoLabel.Text = "END";
                    });
                }
                catch (Exception e)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        infoLabel.Text = e.Message;
                    });
                }
            });
            task.Start();
        }

        void OnDeleteButtonClicked(object sender, EventArgs args)
        {

        }
    }
}
