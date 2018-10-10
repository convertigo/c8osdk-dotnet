using Convertigo.SDK.Internal;
using System;
using System.Threading;
using System.Windows;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init(Action<Action> uiDispatcher = null)
        {
            C8o.deviceUUID = new HardwareHelper().GetHardwareID();
            C8oHTTPsProxy.Init();
            C8oPlatformCommon.Init();
        }
    }
}
