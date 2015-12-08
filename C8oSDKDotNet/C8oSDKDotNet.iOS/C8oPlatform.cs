using Foundation;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            C8o.UiDispatcher = new NSObject().BeginInvokeOnMainThread;
            C8oFullSyncCbl.Init();
        }
    }
}
