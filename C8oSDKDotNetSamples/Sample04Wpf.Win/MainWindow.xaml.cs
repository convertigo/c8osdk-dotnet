using Convertigo.SDK;
using System.IO;
using System.Net;
using System.Windows;

namespace Sample04Wpf.Win
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal C8o c8o;
        internal C8oFileTransfer fileTransfer;

        public MainWindow()
        {
            InitializeComponent();

            C8oPlatform.Init();

            int cl = ServicePointManager.DefaultConnectionLimit = 10;
            ContentArea.Content = new Login(this);

            //var cert = File.ReadAllBytes("D:\\COMMON\\C8O\\7.3.3_srv_win\\tomcat\\conf\\client.p12");

            c8o = new C8o(
            // "https://tonus.twinsoft.fr:28443/convertigo/projects/BigFileTransferSample",
            // "http://localhost:18080/convertigo/projects/BigFileTransferSample",
            "http://192.168.100.69:18080/convertigo/projects/BigFileTransferSample",
            // "http://192.168.100.156:28080/convertigo/projects/BigFileTransferSample",
            //"http://nicolasa.convertigo.net/cems/projects/BigFileTransferSample"
            new C8oSettings()
            .SetTrustAllCertificates(false)
                .AddClientCertificate("clientDassault.p12", "bhytrd")
                .SetFullSyncLocalSuffix("_app2")
                //.AddClientCertificate(cert, "password"));
            );
            fileTransfer = new C8oFileTransfer(c8o);

            fileTransfer.RaiseDebug += (sender, debug) =>
            {
                c8o.RunUI(() =>
                {
                    OutputArea.Text = debug;
                });
            };
        }
    }
}
