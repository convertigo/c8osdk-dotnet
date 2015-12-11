using Convertigo.SDK;
////using Convertigo.SDK.Listeners;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Xamarin.Forms;

namespace retail_store
{
    public partial class Cart : ContentPage
    {

        public Cart()
        {
            InitializeComponent();
            
            labReCo.BindingContext = (ReduceTot)App.cvm.Reduce[0];
            labRePr.BindingContext = (ReduceTot)App.cvm.Reduce[0];
            labReNewP.BindingContext = (ReduceTot)App.cvm.Reduce[0];
            labReDis.BindingContext = (ReduceTot)App.cvm.Reduce[0];


           
        

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
            if (App.cvm.ProductStock != null)
            {
                listView.ItemsSource = App.cvm.ProductStock;
            }
            
            
        }


        protected override void OnAppearing()
        {
            
            App.cvm.GetReducePrice();
            GetView();
        }

        void tapImage_Tapped(object sender, EventArgs e)
        {
            string sku = ((ViewCell)((Image)sender).Parent.Parent.Parent.Parent.Parent.Parent).ClassId.ToString();
            
            string imageName = ((FileImageSource)((Image)sender).Source).File.ToString();
            switch (imageName)
            {
                case "plus.png":
                    App.cvm.SetProductBySku(sku);
                    App.cvm.CheckCart(true);
                    break;
                case "moins.png":
                    App.cvm.SetProductBySku(sku);
                    App.cvm.CheckCart(false);
                    break;
            }

            App.cvm.GetReducePrice();
            

        }
        public void sal(object sender, EventArgs e)
        {
            App.cvm.deleteCart(true);
        }


    }  
}
