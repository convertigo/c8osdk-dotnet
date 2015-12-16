﻿using Convertigo.SDK.Internal;
using System.Windows;

namespace Convertigo.SDK
{
    public class C8oPlatform
    {
        static public void Init()
        {
            var dispatcher = Application.Current.MainWindow.Dispatcher;

            C8o.defaultUiDispatcher = code =>
            {
                dispatcher.BeginInvoke(code);
            };

            C8oHTTPsProxy.Init();
            C8oPlatformCommon.Init();
        }
    }
}
