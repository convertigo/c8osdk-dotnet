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
            listView.ItemsSource = App.cvm.ProductStock;
            listView.SeparatorColor = Color.Black;
        }

        public void SetVisibility(bool visible)
        {
            if (visible)
            {
                labReNewP.IsVisible = true;
                labReDis.IsVisible = true;
            }
            else
            {
                labReNewP.IsVisible = false;
                labReDis.IsVisible = false;
            }
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
        public void a()
        {
            if (App.cvm.ProductStock != null)
            {
                listView.ItemsSource = App.cvm.ProductStock;
            }
        }

        public void refresh()
        {
            GetView();
            App.cvm.GetReducePrice();
        }
        protected override void OnAppearing()
        {
            refresh();
        }

        void tapImage_Tapped(object sender, EventArgs e)
        {
            string id = ((ViewCell)((Image)sender).Parent.Parent.Parent.Parent.Parent.Parent).ClassId.ToString();
            
            string imageName = ((FileImageSource)((Image)sender).Source).File.ToString();
            switch (imageName)
            {
                case "plus.png":
                    App.cvm.SetProductBySku(id);
                    App.cvm.CheckCart(true);

                    break;
                case "moins.png":
                    App.cvm.SetProductBySku(id);
                    App.cvm.CheckCart(false);

                    break;
                case "del.jpg":
                    App.cvm.SetProductBySku(id);
                    App.cvm.deleteCart(true);

                    break;
            }
        }
        public async void tapImage_dell(object sender, EventArgs e)
        {
            var answer = await DisplayAlert("Alert !", "Would you realy wants to delete these items ?", "Yes", "No");
            if (answer)
            {
                string id = ((ViewCell)((Image)sender).Parent.Parent.Parent.Parent).ClassId.ToString();
                App.cvm.SetProductBySku(id);
                App.cvm.deleteCart(true);
            }

        }
    }  
}
