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
            if (uiDispatcher == null)
            {
                var dispatcher = Application.Current.MainWindow.Dispatcher;

                C8o.defaultUiDispatcher = code =>
                {
                    dispatcher.BeginInvoke(code);
                };

                C8o.defaultIsUi = () =>
                {
                    return Thread.CurrentThread == dispatcher.Thread;
                };
            }
            else
            {
                C8o.defaultUiDispatcher = uiDispatcher;
            }

            C8o.defaultBgDispatcher = (code) => {
                new Thread(() =>
                {
                    code();
                }).Start();
            };

            C8o.deviceUUID = new HardwareHelper().GetHardwareID();

            C8oHTTPsProxy.Init();
            C8oPlatformCommon.Init();
        }
    }
}
