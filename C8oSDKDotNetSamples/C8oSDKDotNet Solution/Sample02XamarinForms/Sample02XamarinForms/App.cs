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
        public App(FileReader fileReader)
        {
            MainPage = new NavigationPage(new C8oInitPage(fileReader));
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
