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
using Convertigo.SDK.Listeners;

namespace Sample01Android
{
    class C8oExceptionListenerImpCustom : C8oExceptionListener
    {
        private TextView textView;
        private Activity activity;

        public C8oExceptionListenerImpCustom(TextView textView, Activity activity)
            : base((exception) =>
            {
                activity.RunOnUiThread(() => textView.Text = exception.Message);
            })
        {
            this.textView = textView;
            this.activity = activity;
        }
    }
}