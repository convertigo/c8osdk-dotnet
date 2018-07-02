using Android.OS;
using Convertigo.SDK.Internal;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            Handler mainLooperHandler = new Handler(Looper.MainLooper);

            C8o.defaultIsUi = () =>
            {
                return Looper.MyLooper() == Looper.MainLooper;
            };

            C8o.defaultUiDispatcher = code =>
            {
                if (C8o.defaultIsUi())
                {
                    code.Invoke();
                }
                else {
                    mainLooperHandler.Post(code);
                }
            };
            
            C8o.deviceUUID = Android.OS.Build.Serial;

            C8oPlatformCommon.Init();
        }
    }
}
