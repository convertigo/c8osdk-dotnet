using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Xml.Linq;
using Convertigo.SDK.Listeners;

namespace Sample01Android
{
    class C8oXMLResponseListenerImp : C8oXmlResponseListener
    {
        private TextView textView;
        private Activity activity;

        public C8oXMLResponseListenerImp(TextView textView, Activity activity)
        {
            this.textView = textView;
            this.activity = activity;
        }

        public void onXmlResponse(XDocument xmlResponse, Dictionary<String, Object> parameters)
        {
            this.activity.RunOnUiThread(() => textView.Text = xmlResponse.ToString());
        }

    }
}