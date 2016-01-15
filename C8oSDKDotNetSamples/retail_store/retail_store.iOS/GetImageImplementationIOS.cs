using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using retail_store.iOS;
[assembly: Xamarin.Forms.Dependency(typeof(GetImageImplementationIOS))]

namespace retail_store.iOS
{
    class GetImageImplementationIOS : IGetImage
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
