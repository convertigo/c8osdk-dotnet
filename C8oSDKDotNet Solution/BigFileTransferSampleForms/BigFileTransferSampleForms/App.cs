using C8oBigFileTransfer;
using Convertigo.SDK.FullSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace BigFileTransferSampleForms
{
    public class App : Application
    {
        public App(FullSyncInterface fullSyncInterface, FileManager fileManager)
        {
            MainPage = new MainPage(fullSyncInterface, fileManager);
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
