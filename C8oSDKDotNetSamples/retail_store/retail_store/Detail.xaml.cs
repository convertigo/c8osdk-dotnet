using Convertigo.SDK;
//using Convertigo.SDK.Listeners;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using Xamarin.Forms;
using Newtonsoft.Json.Linq;
using System.IO;

using System.Drawing;


namespace retail_store
{
    public partial class Detail : ContentPage, INotifyPropertyChanged
    {
        private Prod prod;
        private string count;
        public event PropertyChangedEventHandler PropertyChanged;
        public Detail(Prod prod)
        {
            InitializeComponent();
            this.prod = prod;
            this.BindingContext = Prod;
            //Creating TapGestureRecognizers  
            var tapImage = new TapGestureRecognizer();
            //Binding events  
            tapImage.Tapped += tapImage_Tapped;
            //Associating tap events to the image buttons  
            //Image2.GestureRecognizers.Add(tapImage);
            Image3.GestureRecognizers.Add(tapImage);
            Image2.GestureRecognizers.Add(tapImage);
            if (Device.OS == TargetPlatform.iOS)
            {
                NavigationPage.SetHasNavigationBar(this, true);
            }
            else
            {
                NavigationPage.SetHasNavigationBar(this, false);
            }
            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
               
            }
            GetView();
            searchUnit();
            labelCount.BindingContext = this;
        }
        public async void GetView()
        {
            JObject data = await App.myC8oCart.CallJson(
                "fs://.view",                               //We get here a view from the default project as the project has been define in the endpoint URL.  
                "ddoc", "design",                           //And give here parameters
                "view", "view")
                .Fail((e, p) =>
                {
                    Debug.WriteLine("" + e);        // Handle errors..
                })
                .Async();                           //Async Call

            App.cvm.PopulateData(data, true);


            JObject data2 = await App.myC8o.CallJson(
                        "fs://.get",               //We post here the an item into the cart from the default project as the project has been define in the endpoint URL. 
                        "docid", prod.Id           //And give here parameters
                        )
                        .Fail((e, q) =>
                        {
                            Debug.WriteLine("" + e); // Handle errors..
                        })
                        .Async();                   //Async Call
            if (data2["_attachments"]["img.jpg"]["content_path"] != null)
            {
                /* labelImage.Source = ((byte[])ImageSource.FromUri(new Uri(((string)data2["_attachments"]["img.jpg"]["content_url"])));
                 /* MemoryStream mStream = new MemoryStream();
                  byte[] pData = ((byte[])data2["_attachments"]["img.jpg"]["content_url"]);
                  mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
                  Bitmap bm = new Bitmap(mStream, false);
                  mStream.Dispose();
                  labelImage.Source = Xamarin.Forms.((byte[])data2["_attachments"]["img.jpg"]["content_url"]);*/
                /*
               FileStream fs = null;
               try
               {
                   fs = File.OpenRead(fullFilePath);
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
               byte[] bytes = File.ReadAllBytes((string)data2["_attachments"]["img.jpg"]["content_url"]);
                   Xamarin.Forms.ImageSource a = ImageSource.FromStream(() => new MemoryStream(((byte[])data2["_attachments"]["img.jpg"]["content_url"])));
                   labelImage.Source = a;*/


                /* ((Byte[])data2["_attachments"]["img.jpg"]["content_url"])
                 labelImage.Source = Xamarin.Forms.ImageSource.FromUri(new Uri("file:///data/data/retail_store.Droid/files/.local/share/retailfulldb_device attachments/445D35AB35261309F60F47496A5A0435F7410025.jpg"));
                 */

                byte[] b = DependencyService.Get<IGetImage>().GetMyImage(((string)data2["_attachments"]["img.jpg"]["content_path"]));
                labelImage.Source = ImageSource.FromStream(() => new MemoryStream(b));
                
                
                labelImage.Aspect = Aspect.Fill;
            }



        }

        public void searchUnit()
        {
            foreach (ProdStock item in App.cvm.ProductStock)
            {
                if (item.Id == prod.Id)
                {
                    this.Count = item.Count.ToString();
                    break;
                }
                else
                {
                    this.Count = "0";
                }
            }
            if (Convert.ToInt16(this.Count) < 0 || this.Count == null)
            {
                this.Count = "0";
            }
        }
        

        public void tapImage_Tapped(object sender, EventArgs e)
        {
            string imageName = ((FileImageSource)((Xamarin.Forms.Image)sender).Source).File.ToString();
            switch (imageName)
            {
                case "plus.png":
                    App.cvm.Product = Prod;
                    App.cvm.CheckCart(true);
                    break;
                case "moins.png":
                    App.cvm.Product = Prod;
                    App.cvm.CheckCart(false);
                    break;
            }
            searchUnit();
        }
        protected override void OnAppearing()
        {
            searchUnit();
        }

        //Getters and Setters
        public Prod Prod
        {
            get
            {
                return prod;
            }

            set
            {
                prod = value;
            }
        }

        public string Count
        {
            get
            {
                return count;
            }

            set
            {
                count = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }


    }
}
