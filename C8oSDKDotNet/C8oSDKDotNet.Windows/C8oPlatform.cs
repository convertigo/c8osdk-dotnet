using System.IO;
using System.Windows;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            var dispatcher = Application.Current.MainWindow.Dispatcher;

            C8o.defaultUiDispatcher = code =>
            {
                dispatcher.BeginInvoke(code);
            };

            C8oFileTransfer.fileManager = new C8oFileManager(path =>
            {
                FileStream fileStream = File.Create(path);
                return fileStream;
            }, path =>
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                return fileStream;
            });

            C8oFullSyncCbl.Init();
        }
    }
}
