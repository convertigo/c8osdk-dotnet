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
                "fs://.view",
                "ddoc", "design",
                "view", "view")
                .Fail((e, p) =>
                {
                    // Handle errors..
                })
                .Async();
            App.cvm.PopulateData(data, true);
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

        public void tapImage_Tapped(object sender, EventArgs e)
        {
            string imageName = ((FileImageSource)((Image)sender).Source).File.ToString();
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


    }
}
