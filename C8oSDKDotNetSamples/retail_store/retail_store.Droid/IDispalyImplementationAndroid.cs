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

using retail_store.Droid;
using retail_store;

[assembly: Xamarin.Forms.Dependency(typeof(IDispalyImplementationAndroid))]

namespace retail_store.Droid
{
    class IDispalyImplementationAndroid : IDisplay
    {
        public double Xdpi { get; private set; }

        public double Ydpi { get; private set; }
        public IDispalyImplementationAndroid()
        {
            Xdpi = Application.Context.Resources.DisplayMetrics.Xdpi;
            Ydpi = Application.Context.Resources.DisplayMetrics.Ydpi;
        }
        public Double Height
        {
            get
            {
                return (((Double)DpFromPx(Application.Context.Resources.DisplayMetrics.HeightPixels)) / 1.973);
            }
        }

        public Double Width
        {
            get { return ((Double)DpFromPx(Application.Context.Resources.DisplayMetrics.WidthPixels)) / 1.2; }
        }

        private static float DpFromPx(int px)
        {
            return px / Application.Context.Resources.DisplayMetrics.Density;
        }
    }
}