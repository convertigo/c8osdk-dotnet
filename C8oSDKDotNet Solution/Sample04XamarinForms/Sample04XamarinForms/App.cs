﻿using C8oBigFileTransfer;
using Convertigo.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Sample04XamarinForms
{
    public class App : Application
    {
        internal C8o c8o;
        internal BigFileTransferInterface bigFileTransfer;

        public App(FileManager fileManager)
        {
            // The root page of your application
            MainPage = new Login(this);

            c8o = new C8o("http://tonus.twinsoft.fr:18080/convertigo/projects/BigFileTransferSample");

            bigFileTransfer = new BigFileTransferInterface("http://tonus.twinsoft.fr:18080/convertigo/projects/BigFileTransfer", new C8oSettings()
                .SetDefaultFullSyncDatabaseName("bigfiletransfer")
                .SetFullSyncUsername("admin")
                .SetFullSyncPassword("admin")
            , fileManager);
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
