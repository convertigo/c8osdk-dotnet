using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Convertigo.SDK.FullSync;
using System.IO;
using Java.Net;
using Java.IO;

namespace Sample02XamarinForms.Droid
{
    [Activity(Label = "Sample02XamarinForms", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            FileReader androidFileReader = new FileReader((filePath) =>
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return fileBytes;
            });

            LoadApplication(new App(androidFileReader));        
        }
    }
}

