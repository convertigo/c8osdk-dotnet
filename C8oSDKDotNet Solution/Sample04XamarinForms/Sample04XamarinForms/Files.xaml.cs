using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Convertigo.SDK.Listeners;

using Xamarin.Forms;
using System.Diagnostics;

namespace Sample04XamarinForms
{
    public partial class Files : ContentPage
    {
        class File
        {
            internal String name;
            String size;

            internal File(String name, String size)
            {
                this.name = name;
                this.size = size;
            }

            public override String ToString()
            {
                return name + " [" + size + "]";
            }
        }

        App app;
        List<File> files = new List<File>();
        List<File> progressFiles = new List<File>();

        public Files(App app)
        {
            InitializeComponent();

            this.app = app;

            FilesList.ItemsSource = files;
            FilesListProgress.ItemsSource = progressFiles;

            app.c8o.Call(".Files", null, new C8oJsonResponseListener((filesResponse, param) =>
            {
                Debug.WriteLine(filesResponse.ToString());
                
                foreach (var file in filesResponse.SelectTokens("document.directory.file[*]"))
                {
                    files.Add(new File(file["text"].ToString(), file.SelectToken("attr.size").ToString()));
                }
                
                Device.BeginInvokeOnMainThread(() =>
                {
                    FilesList.ItemsSource = null;
                    FilesList.ItemsSource = files;
                    if (files.Count > 0)
                    {
                        FilesList.SelectedItem = files[0];
                    }
                });
            }));
        }

        private void DownloadButtonClick(Object sender, EventArgs args)
        {
            File file = FilesList.SelectedItem as File;

            files.Remove(file);
            progressFiles.Add(file);
            Device.BeginInvokeOnMainThread(() =>
            {
                FilesList.ItemsSource = null;
                FilesList.ItemsSource = files;
                if (files.Count > 0)
                {
                    FilesList.SelectedItem = files[0];
                }
                FilesListProgress.ItemsSource = null;
                FilesListProgress.ItemsSource = progressFiles;
            });

            app.c8o.Call(".RequestFile", new Dictionary<String, Object>
            {
                {"filename", file.name}
            }, new C8oJsonResponseListener((jsonResponse, param) =>
            {
                Debug.WriteLine(jsonResponse.ToString());

                String uuid = jsonResponse.SelectToken("document.uuid").ToString();

                app.bigFileTransfer.DownloadFile(uuid, "/tmp/BigFileDemo/" + param["filename"], (progress) =>
                {
                    Debug.WriteLine(progress);
                }, (progress) =>
                {

                }).ContinueWith(none =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        FilesList.ItemsSource = null;
                        FilesList.ItemsSource = files;
                        FilesListProgress.ItemsSource = null;
                        FilesListProgress.ItemsSource = progressFiles;
                    });
                    progressFiles.Remove(file);
                    files.Add(file);
                });
            }));
        }
    }
}
