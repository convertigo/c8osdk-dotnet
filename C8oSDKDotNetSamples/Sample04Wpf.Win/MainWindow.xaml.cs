using Convertigo.SDK;
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

            c8o = new C8o(
                "http://tonus.twinsoft.fr:18080/convertigo/projects/BigFileTransferSample"
                //"http://nicolasa.convertigo.net/cems/projects/BigFileTransferSample"
            );

            fileTransfer = new C8oFileTransfer(c8o);

            fileTransfer.RaiseDebug += (object sender, string debug) =>
            {
                c8o.RunUI(() =>
                {
                    OutputArea.Text = debug;
                });
            };
        }
    }
}
