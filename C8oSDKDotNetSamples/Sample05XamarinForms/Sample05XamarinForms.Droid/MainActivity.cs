using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Sample05Shared;
using System.IO;

namespace Sample05XamarinForms.Droid
{
	[Activity (Label = "Sample05XamarinForms", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            using (var memstream = new MemoryStream())
            {
                Assets.Open("client.p12").CopyTo(memstream);
                Sample05.cert = memstream.ToArray();
            }
			global::Xamarin.Forms.Forms.Init (this, bundle);
			LoadApplication (new Sample05XamarinForms.App ());
		}
	}
}

