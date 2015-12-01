using Convertigo.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Sample05XamarinForms
{
	public class App : Application
	{
		public App ()
		{
            C8oPlatform.Init();
			// The root page of your application
            MainPage = new TestingPage();
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
