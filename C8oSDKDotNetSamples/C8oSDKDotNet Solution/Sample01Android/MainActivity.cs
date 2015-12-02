using System;
using System.Collections.Generic;
using Convertigo.SDK;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android;

using Convertigo.SDK.FullSync;
using C8oFullSyncNetAndroid;

using Couchbase.Lite;


namespace Sample01Android
{
    [Activity(Label = "Sample01Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            // UI
            Button button1 = FindViewById<Button>(Resource.Id.button1Id);
            Button button2 = FindViewById<Button>(Resource.Id.button2Id);
            TextView textView1 = FindViewById<TextView>(Resource.Id.textView1Id);
            TextView textView2 = FindViewById<TextView>(Resource.Id.textView2Id);

            // C8o objects
            // String endpoint = "http://trial.convertigo.net.error/cems/projects/TestClientSDK";
            // String endpoint = "https://192.168.100.179:28081/convertigo/projects/TestClientSDK";
            String endpoint = "http://192.168.100.86:18080/convertigo/projects/TestClientSDK";
            C8oSettings c8oSettings = new C8oSettings().setTimeout(10000);
            c8oSettings.addCookie("TESTCOOKIENAME", "TESTCOOKIEVALUE");

            c8oSettings.defaultFullSyncDatabaseName = "testclientsdk_fullsync";
            c8oSettings.fullSyncInterface = new FullSyncMobile();

            C8oJSONResponseListenerImp c8oJSONResponseListener = new C8oJSONResponseListenerImp(textView1, this);
            C8oXMLResponseListenerImp c8oXMLResponseListener = new C8oXMLResponseListenerImp(textView2, this);
            C8oExceptionListenerImpDefault c8oExceptionListenerImpDefault = new C8oExceptionListenerImpDefault(this);
            C8oExceptionListenerImpCustom c8oExceptionListenerImpCustom = new C8oExceptionListenerImpCustom(textView1, this);

            C8o c8o = new C8o(endpoint, c8oSettings, c8oExceptionListenerImpDefault);

            // TMP FullSync

            // !!! Need to import Couchbase.Lite and add using Couchbase.Lite;

            C8oFullSyncResponseListener fullSyncResponseListener = new C8oFullSyncResponseListener((document, parameters) =>
            {
                String str = "OnDocumentResponse";
                this.RunOnUiThread(() => textView2.Text = str);
            }, (queryEnumerator, parameters) =>
            {
                String str = "OnQueryEnumeratorResponse";
                this.RunOnUiThread(() => textView2.Text = str);
            }, (replicationChangeEventArgs, parameters) =>
            {
                int changesCount = replicationChangeEventArgs.Source.ChangesCount;
                ReplicationStatus replicationStatus = replicationChangeEventArgs.Source.Status;
                String str = "OnReplicationChangeEventResponse changesCount=" + changesCount + 
                    " replicationStatus=" + replicationStatus;
                this.RunOnUiThread(() => textView2.Text = str);
            });

            // TMP log
            //c8o.log(C8oLogLevel.ERROR, "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST");

            // Events
            button1.Click += delegate
            {
                textView1.Text = "test 01";
                //c8o.call(new Dictionary<string, Object>
                //    {
                //        {"__connector", "http_connector"},
                //        {"__transaction", "transac1"},
                //        {"testvariable", "test 01"}
                //    }, c8oJSONResponseListener, c8oExceptionListenerImpCustom);
                c8o.Call(new Dictionary<String, Object>
                {
                    {"__project", "fs://"},
                    {"__sequence", "replicate_pull"},
                    {"testVariable", "TEST 01"}
                }, fullSyncResponseListener, c8oExceptionListenerImpCustom);
            };

            button2.Click += delegate
            {
                textView2.Text = "TEST 02";
                c8o.Call(".HTTP_connector.transac2", new Dictionary<string, Object>
                {
                    {"testVariable", "TEST 02"}
                }, c8oXMLResponseListener);
                //c8o.call(new Dictionary<String, Object>
                //{
                //    {"__project", "fs://"},
                //    {"__sequence", "all"},
                //    {"testVariable", "TEST 01"}
                //}, fullSyncResponseListener, c8oExceptionListenerImpCustom);
            };
        }
    }
}

