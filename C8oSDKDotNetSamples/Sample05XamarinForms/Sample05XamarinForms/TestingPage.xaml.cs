using Newtonsoft.Json.Linq;
using Sample05Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sample05XamarinForms
{
	public partial class TestingPage : ContentPage
	{
        Sample05 common;

		public TestingPage ()
		{
			InitializeComponent();

            common = new Sample05(
                Device.BeginInvokeOnMainThread,
                (output) =>
                {
                    Output.Text = output;
                },
                (debug) =>
                {
                    System.Diagnostics.Debug.WriteLine(debug);
                }
            );
		}

        private async void OnTest01(object sender, EventArgs args)
        {
            await common.OnTest01(sender, args);
        }

        private async void OnTest02(object sender, EventArgs args)
        {
            await common.OnTest02(sender, args);
        }

        private void OnTest03(object sender, EventArgs args)
        {
            common.OnTest03(sender, args);
        }

        private void OnTest04(object sender, EventArgs args)
        {
            common.OnTest04(sender, args);
        }

        private void OnTest05(object sender, EventArgs args)
        {
            common.OnTest05(sender, args);
        }
    }
}
