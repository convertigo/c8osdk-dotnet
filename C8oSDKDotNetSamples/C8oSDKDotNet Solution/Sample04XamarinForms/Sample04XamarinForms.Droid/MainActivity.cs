using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using C8oBigFileTransfer;
using System.IO;
using Convertigo.SDK.FullSync;

namespace Sample04XamarinForms.Droid
{
    [Activity(Label = "Sample04XamarinForms", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            FullSyncMobile.Init();
            LoadApplication(new App(new FileManager((path) =>
            {
                FileStream fileStream = File.Create(path);
                return fileStream;
            }, (path) =>
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                return fileStream;
            })));
        }
    }
}

