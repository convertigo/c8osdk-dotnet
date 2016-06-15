using Convertigo.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

                app.fileTransfer.RaiseTransferStatus += (sender, transferStatus) =>
                {
                    app.c8o.RunUI(() =>
                    {
                        if (transferStatus.isDownload)
                        {
                            File file = null;
                            foreach (File item in progressFiles)
                            {
                                if (transferStatus.Uuid == item.uuid)
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
                                    if (transferStatus.Filepath.EndsWith(item.name))
                                    {
                                        file = item;
                                        file.uuid = transferStatus.Uuid;
                                        files.Remove(item);
                                        FilesList.ItemsSource = null;
                                        FilesList.ItemsSource = files;
                                        break;
                                    }
                                }
                            }

                            if (transferStatus.State == C8oFileTransferStatus.StateFinished)
                            {
                                file.progress = "";
                                files.Add(file);
                                FilesList.ItemsSource = null;
                                FilesList.ItemsSource = files;
                            }
                            else
                            {
                                file.progress = transferStatus.State.ToString();
                                if (transferStatus.State == C8oFileTransferStatus.StateReplicate)
                                {
                                    file.progress += " " + transferStatus.Current + "/" + transferStatus.Total + " (" + transferStatus.Progress + ")";
                                }
                                progressFiles.Add(file);
                                FilesListProgress.ItemsSource = null;
                                FilesListProgress.ItemsSource = progressFiles;
                            }
                        } else
                        {
                            var i = 0;
                        }
                    });
                };
                app.fileTransfer.Start();

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
                await app.fileTransfer.DownloadFile(file.uuid, path + file.uuid + "_" + file.name);
            }
        }

        private async void UploadButtonClick(Object sender, EventArgs args)
        {
            Assembly assembly = this.GetType().Assembly;
            string projectName =  assembly.GetName().Name;
            string fileName = "FileToUpload10M.gif";
            Stream fileStream = assembly.GetManifestResourceStream(projectName + "." + fileName);
            
            await app.fileTransfer.UploadFile(fileName, fileStream);
        }
    }
}
