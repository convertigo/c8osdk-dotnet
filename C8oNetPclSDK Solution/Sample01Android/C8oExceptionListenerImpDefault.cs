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
    class C8oExceptionListenerImpDefault : C8oExceptionListener
    {
        public Context context;

        public C8oExceptionListenerImpDefault(Context context)
            : base((exception) =>
            {
                Toast toast = Toast.MakeText(context, exception.Message, ToastLength.Short);
                toast.Show();
            })
        {
            this.context = context;
        }
    }
}