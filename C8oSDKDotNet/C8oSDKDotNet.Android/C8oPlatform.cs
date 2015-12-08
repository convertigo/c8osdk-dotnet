using Android.OS;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            Handler mainLooperHandler = new Handler(Looper.MainLooper);

            C8o.UiDispatcher = code =>
            {
                if (Looper.MyLooper() == Looper.MainLooper)
                {
                    code.Invoke();
                }
                else {
                    mainLooperHandler.Post(code);
                }
            };
            C8oFullSyncCbl.Init();
        }
    }
}
