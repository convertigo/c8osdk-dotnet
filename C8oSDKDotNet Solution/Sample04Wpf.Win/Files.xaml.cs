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
using Convertigo.SDK.Listeners;

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

        MainWindow app;

        public Files(MainWindow app)
        {
            InitializeComponent();
            
            this.app = app;

            app.c8o.Call(".Files", null, new C8oXmlResponseListener((filesResponse, param) =>
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
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            File file = FilesList.SelectedItem as File;

            FilesList.Items.Remove(file);
            FilesListProgress.Items.Add(file);

            app.c8o.Call(".RequestFile", new Dictionary<String, Object>
            {
                {"filename", file.name}
            }, new C8oXmlResponseListener((xmlResponse, param) =>
            {
                String xml = xmlResponse.ToString();

                Dispatcher.Invoke(() =>
                {
                    app.OutputArea.Text = xml;
                });

                String uuid = xmlResponse.XPathSelectElement("/document/uuid").Value;
                
                app.bigFileTransfer.DownloadFile(uuid, "D:\\TMP\\" + param["filename"], (progress) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        app.OutputArea.Text = progress;
                    });
                }, (progress) =>
                {

                }).ContinueWith(none =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FilesListProgress.Items.Remove(file);
                        FilesList.Items.Add(file);
                    });
                });
            }));
        }
    }
}
