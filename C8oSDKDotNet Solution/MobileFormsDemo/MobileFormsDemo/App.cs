using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

using Convertigo.SDK;
using Convertigo.SDK.FullSync;
using Convertigo.SDK.Listeners;


namespace MobileFormsDemo
{
    public class App : Application
    {
        public static C8o myC8o;
        public static Dictionary<String, Object> models;

        public App()
        {
            myC8o = new C8o("http://52.3.148.154/convertigo/projects/SalesforceFullSync",
                new C8oSettings().
                    SetTimeout(10000).
                    SetTrustAllCertificates(true).
                    SetDefaultFullSyncDatabaseName("salesforcefullsync_fullsync").
                    SetIsLogRemote(true)
            );

            models = new Dictionary<string, object>();

            // The root page of your application
            MainPage = new ListPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            myC8o.Call(".login",
                new Dictionary<string, object> {
                    { "username", "demo@twinsoft.fr" },
                    { "password", "d3m0tw1n" }
                },
                new C8oJsonResponseListener((jsonResponse, parameters) =>
                {
                    myC8o.Log(C8oLogLevel.DEBUG, jsonResponse.ToString());
                })
            );
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
