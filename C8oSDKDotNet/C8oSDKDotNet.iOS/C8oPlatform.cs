using Foundation;
using System.IO;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            C8o.defaultUiDispatcher = new NSObject().BeginInvokeOnMainThread;

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
