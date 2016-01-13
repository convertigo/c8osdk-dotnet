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
using System.IO;
using Xamarin.Forms;

//
using retail_store.Droid;
[assembly: Xamarin.Forms.Dependency(typeof(GetImageImplementation))]


namespace retail_store.Droid
{
    public class GetImageImplementation : IGetImage
    {
        public byte[] GetMyImage(string text)
        {
            FileStream fs = null;
            try
            {
                fs = File.OpenRead(text);
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                
                return bytes;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }
    }
}