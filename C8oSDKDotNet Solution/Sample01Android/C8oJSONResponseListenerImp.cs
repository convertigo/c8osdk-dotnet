using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Convertigo.SDK.Listeners;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Sample01Android
{
    class C8oJSONResponseListenerImp : C8oJsonResponseListener
    {
        private TextView textView;
        private Activity activity;

        public C8oJSONResponseListenerImp(TextView textView, Activity activity)
            : base((jsonResponse, parameters) =>
            {
                String str = "NULL";
                if (jsonResponse != null)
                {
                    str = jsonResponse.ToString();
                }
                activity.RunOnUiThread(() => textView.Text = str);
            })
        {
            this.textView = textView;
            this.activity = activity;
        }
    }
}
