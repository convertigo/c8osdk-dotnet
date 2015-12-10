using C8oBigFileTransfer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;

namespace SampleBigFileTransfer
{
    public partial class Files : ContentPage
    {
        class File
        {
            internal string name;
            string size;
            internal string progress = "";
            internal string uuid;

            internal File(string name, string size)
            {
                this.name = name;
                this.size = size;
            }

            public override string ToString()
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

            app.c8o.CallJson(".Files").Then((response, param) =>
            {
                Debug.WriteLine(response.ToString());

                foreach (var file in response.SelectTokens("document.directory.file[*]"))
                {
                    files.Add(new File(file["text"].ToString(), file.SelectToken("attr.size").ToString()));
                }

                app.c8o.RunUI(() =>
                {
                    FilesList.ItemsSource = null;
                    FilesList.ItemsSource = files;
                    if (files.Count > 0)
                    {
                        FilesList.SelectedItem = files[0];
                    }
                });

                app.bigFileTransfer.RaiseDownloadStatus += (sender, downloadStatus) =>
                {
                    app.c8o.RunUI(() =>
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

                return null;
            });
        }

        private async void DownloadButtonClick(Object sender, EventArgs args)
        {
            File file = FilesList.SelectedItem as File;

            file.progress = "preparing";

            files.Remove(file);
            progressFiles.Add(file);
            app.c8o.RunUI(() =>
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

            var json = await app.c8o.CallJson(".RequestFile", "filename", file.name).Async();
            Debug.WriteLine(json.ToString());

            var uuid = json.SelectToken("document.uuid");
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

            if (uuid != null)
            {
                file.uuid = uuid.ToString();
                string path = Device.OS == TargetPlatform.Android ? "/sdcard/Download/" : "/tmp/";
                await app.bigFileTransfer.AddFile(file.uuid, path + file.uuid + "_" + file.name);
            }
        }
    }
}
