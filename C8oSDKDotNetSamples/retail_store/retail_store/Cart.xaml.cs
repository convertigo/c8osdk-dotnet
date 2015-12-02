using Convertigo.SDK;
using Convertigo.SDK.Listeners;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            App.myC8o.Call("fs://cartdb.view",
                new Dictionary<string, object>
                {
                    {"ddoc", "design"},
                    {"view", "view"}

                },
                new C8oJsonResponseListener((jsonResponse, parameters) =>
                {
                    App.cvm.PopulateData(jsonResponse, true);
                    listView.ItemsSource = App.cvm.ProductStock;

                }),

                new C8oExceptionListener((exception, parameters) =>
                {
                    Debug.WriteLine("Exeption : [ToString] = " + exception.ToString() + "Fin du [ToString]");
                })
            );
        }

        protected override void OnAppearing()
        {
            App.cvm.GetReducePrice();
            App.cvm.GetRealPrice();
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
            App.cvm.GetRealPrice();

        }
        public void sal(object sender, EventArgs e)
        {
            App.cvm.deleteCart(true);
            //App.cvm.GetReducePrice();
        }


    }  
}
