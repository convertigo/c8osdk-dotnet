using Android.OS;

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

            C8o.deviceUUID = Android.OS.Build.Serial;

            C8oFullSyncCbl.Init();
        }
    }
}
