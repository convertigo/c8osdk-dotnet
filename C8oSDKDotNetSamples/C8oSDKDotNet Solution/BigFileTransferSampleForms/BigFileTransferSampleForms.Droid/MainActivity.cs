using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Convertigo.SDK.FullSync;
using C8oBigFileTransfer;
using System.IO;

namespace BigFileTransferSampleForms.Droid
{
    [Activity(Label = "BigFileTransferSampleForms", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            //FileManager fileManager = new FileManager((filePath) =>
            //{
            //    return File.ReadAllBytes(filePath);
            //}, (filePath, fileData) =>
            //{
            //    File.WriteAllBytes(filePath, fileData);
            //});
            FileManager fileManager = new FileManager((path) =>
            {
                FileStream fileStream = File.Create(path);
                return fileStream;
            }, (path) =>
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                return fileStream;
            });

            LoadApplication(new App(new FullSyncMobile(), fileManager));
        }
    }
}

