using Convertigo.SDK.Internal;
using System;
using System.Windows;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init(Action<Action> uiDispatcher = null)
        {
            if (uiDispatcher == null)
            {
                var dispatcher = Application.Current.MainWindow.Dispatcher;

                C8o.defaultUiDispatcher = code =>
                {
                    dispatcher.BeginInvoke(code);
                };
            }
            else
            {
                C8o.defaultUiDispatcher = uiDispatcher;
            }

            C8o.deviceUUID = new HardwareHelper().GetHardwareID();

            C8oHTTPsProxy.Init();
            C8oPlatformCommon.Init();
        }
    }
}
