using Convertigo.SDK.Internal;
using Foundation;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            C8o.defaultIsUi = () =>
            {
                return NSThread.IsMain;
            };

            C8o.defaultUiDispatcher = new NSObject().BeginInvokeOnMainThread;

            C8oPlatformCommon.Init();
        }
    }
}
