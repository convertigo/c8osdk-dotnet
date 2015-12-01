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
using System.Xml.Linq;
using System.Xml.XPath;
using Convertigo.SDK;
using C8oBigFileTransfer;

namespace Sample04Wpf.Win
{
    /// <summary>
    /// Interaction logic for Files.xaml
    /// </summary>
    public partial class Files : UserControl
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
                return name + " [" + size + "] " + progress;
            }

        }

        MainWindow app;

        public Files(MainWindow app)
        {
            InitializeComponent();
            
            this.app = app;

            app.c8o.Call(".Files", null, new C8oResponseXmlListener((filesResponse, param) =>
            {
                String xml = filesResponse.ToString();
                IEnumerable<XElement> files = filesResponse.XPathSelectElements("/document/directory/file");

                Dispatcher.Invoke(() =>
                {
                    app.OutputArea.Text = xml;

                    foreach (XElement file in files)
                    {
                        FilesList.Items.Add(new File(file.Value, file.Attribute("size").Value));
                    }

                    FilesList.SelectedIndex = 0;
                });
            }));

            app.bigFileTransfer.RaiseDownloadStatus += (Object sender, DownloadStatus downloadStatus) =>
            {
                Dispatcher.Invoke(() =>
                {
                    File file = null;

                    foreach (File item in FilesListProgress.Items)
                    {
                        if (downloadStatus.Uuid == item.uuid)
                        {
                            file = item;
                            FilesListProgress.Items.Remove(item);
                            break;
                        }
                    }

                    if (file == null)
                    {
                        foreach (File item in FilesList.Items)
                        {
                            if (downloadStatus.Filepath.EndsWith(item.name))
                            {
                                file = item;
                                file.uuid = downloadStatus.Uuid;
                                FilesList.Items.Remove(item);
                                break;
                            }
                        }
                    }

                    if (downloadStatus.State == DownloadStatus.StateFinished)
                    {
                        file.progress = "";
                        FilesList.Items.Add(file);
                    }
                    else
                    {
                        file.progress = downloadStatus.State.ToString();
                        if (downloadStatus.State == DownloadStatus.StateReplicate)
                        {
                            file.progress += " " + downloadStatus.Current + "/" + downloadStatus.Total + " (" + downloadStatus.Progress + ")";
                        }
                        FilesListProgress.Items.Add(file);
                    }
                });
            };

            app.bigFileTransfer.Start();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            File file = FilesList.SelectedItem as File;

            file.progress = "preparing";

            FilesList.Items.Remove(file);
            FilesListProgress.Items.Add(file);

            app.c8o.Call(".RequestFile", new Dictionary<String, Object>
            {
                {"filename", file.name}
            }, new C8oResponseXmlListener(async (xmlResponse, param) =>
            {
                String xml = xmlResponse.ToString();
                XElement uuid = xmlResponse.XPathSelectElement("/document/uuid");

                Dispatcher.Invoke(() =>
                {
                    app.OutputArea.Text = xml;
                    if (uuid == null)
                    {
                        file.progress = "error";

                        FilesListProgress.Items.Remove(file);
                        FilesList.Items.Add(file);
                    }
                });

                if (uuid != null)
                {
                    file.uuid = uuid.Value;
                    await app.bigFileTransfer.AddFile(file.uuid, "D:\\TMP\\" + file.uuid + "_" + param["filename"]);
                }
            }));
        }
    }
}
