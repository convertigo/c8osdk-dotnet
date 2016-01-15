using System;
using System.Collections.Generic;
using System.Text;

using retail_store.iOS;
using retail_store;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(IDisplayimplementationIos))]
namespace retail_store.iOS
{
    public class IDisplayimplementationIos : IDisplay
    {

        public IDisplayimplementationIos()
        {

        }
        public Double Height
        {
            get
            {
                if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.Portrait)
                {
                    return (UIScreen.MainScreen.Bounds.Size.Height / 2.1);
                }
                else
                {
                    return UIScreen.MainScreen.Bounds.Size.Height / 2gg.1;
                }
                
            }
        }

        public Double Width
        {
            get
            {

                if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.Portrait)
                {
                    return UIScreen.MainScreen.Bounds.Size.Width / 1.2;
                }
                else
                {
                    return UIScreen.MainScreen.Bounds.Size.Width / 3;
                }
                
            }
        }
    }

}



