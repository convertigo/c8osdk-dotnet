using Android.OS;
using System.IO;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            Handler mainLooperHandler = new Handler(Looper.MainLooper);

            C8o.defaultUiDispatcher = code =>
            {
                if (Looper.MyLooper() == Looper.MainLooper)
                {
                    code.Invoke();
                }
                else {
                    mainLooperHandler.Post(code);
                }
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

            C8o.deviceUUID = Android.OS.Build.Serial;

            C8oFullSyncCbl.Init();
        }
    }
}
