using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Convertigo.SDK;
using C8oBigFileTransfer;

using Xamarin.Forms;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Sample04XamarinForms
{
    public partial class Files : ContentPage
    {
        class File
        {
            internal String name;
            String size;
            internal String progress = "";
            internal String uuid;

            internal File(String name, String size)
            {
                this.name = name;
                this.size = size;
            }

            public override String ToString()
            {
                return name + " " + (progress.Length > 0 ? progress : "[" + size + "]");
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

            app.c8o.Call(".Files", null, new C8oResponseJsonListener((filesResponse, param) =>
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

                app.bigFileTransfer.RaiseDownloadStatus += (Object sender, DownloadStatus downloadStatus) =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        File file = null;

                        foreach (File item in progressFiles)
                        {
                            if (downloadStatus.Uuid == item.uuid)
                            {
                                file = item;
                                progressFiles.Remove(item);
                                FilesListProgress.ItemsSource = null;
                                FilesListProgress.ItemsSource = progressFiles;
                                break;
                            }
                        }

                        if (file == null)
                        {
                            foreach (File item in files)
                            {
                                if (downloadStatus.Filepath.EndsWith(item.name))
                                {
                                    file = item;
                                    file.uuid = downloadStatus.Uuid;
                                    files.Remove(item);
                                    FilesList.ItemsSource = null;
                                    FilesList.ItemsSource = files;
                                    break;
                                }
                            }
                        }

                        if (downloadStatus.State == DownloadStatus.StateFinished)
                        {
                            file.progress = "";
                            files.Add(file);
                            FilesList.ItemsSource = null;
                            FilesList.ItemsSource = files;
                        }
                        else
                        {
                            file.progress = downloadStatus.State.ToString();
                            if (downloadStatus.State == DownloadStatus.StateReplicate)
                            {
                                file.progress += " " + downloadStatus.Current + "/" + downloadStatus.Total + " (" + downloadStatus.Progress + ")";
                            }
                            progressFiles.Add(file);
                            FilesListProgress.ItemsSource = null;
                            FilesListProgress.ItemsSource = progressFiles;
                        }
                    });
                };

                app.bigFileTransfer.Start();
            }));
        }

        private void DownloadButtonClick(Object sender, EventArgs args)
        {
            File file = FilesList.SelectedItem as File;

            file.progress = "preparing";

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
            }, new C8oResponseJsonListener(async (jsonResponse, param) =>
            {
                Debug.WriteLine(jsonResponse.ToString());

                JToken uuid = jsonResponse.SelectToken("document.uuid");
   
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (uuid == null)
                    {
                        file.progress = "error";

                        FilesList.ItemsSource = null;
                        FilesList.ItemsSource = files;
                        FilesListProgress.ItemsSource = null;
                        FilesListProgress.ItemsSource = progressFiles;

                        progressFiles.Remove(file);
                        files.Add(file);
                    }
                });

                if (uuid != null)
                {
                    file.uuid = uuid.ToString();
                    String path = Device.OS == TargetPlatform.Android ? "/sdcard/Download/" : "/tmp/";
                    await app.bigFileTransfer.AddFile(file.uuid, path + file.uuid + "_" + param["filename"]);
                }
            }));
        }
    }
}
