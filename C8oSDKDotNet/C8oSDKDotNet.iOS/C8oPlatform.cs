using Foundation;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            C8o.defaultUiDispatcher = new NSObject().BeginInvokeOnMainThread;
            C8oFullSyncCbl.Init();
        }
    }
}
