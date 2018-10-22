using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using App1.Views;
using Convertigo.SDK;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace App1
{
    public partial class App : Application
    {
        public C8o c8o = new C8o("http://c8o-dev.convertigo.net/cems/projects/ClientSDKtesting", new C8oSettings()
            .SetFullSyncStorageEngine(C8o.FS_STORAGE_SQL)
        );

        public App()
        {
            InitializeComponent();


            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
