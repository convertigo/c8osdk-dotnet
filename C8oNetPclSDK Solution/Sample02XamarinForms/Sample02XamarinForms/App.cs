using Convertigo.SDK;
using Convertigo.SDK.FullSync;
using Convertigo.SDK.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Sample02XamarinForms
{
    public class App : Application
    {

        private FullSyncInterface fullSyncInterface;
        private FileReader fileReader;

        public App(FullSyncInterface fullSyncInterface, FileReader fileReader)
        {
            this.fullSyncInterface = fullSyncInterface;
            this.fileReader = fileReader;
            MainPage = new C8oCallPage();
        }

        protected override void OnStart()
        {
            (MainPage as C8oCallPage).Init(this.fullSyncInterface, this.fileReader);
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
