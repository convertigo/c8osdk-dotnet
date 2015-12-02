using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using C8oBigFileTransfer;
using System.IO;
using Convertigo.SDK.FullSync;

namespace Sample04XamarinForms.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            FullSyncMobile.Init();

            FileManager fileManager = new FileManager((path) =>
            {
                // String myDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                FileStream fileStream = File.Create(path);
                return fileStream;
            }, (path) =>
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                return fileStream;
            });

            LoadApplication(new App(fileManager));

            return base.FinishedLaunching(app, options);
        }
    }
}
