﻿using Convertigo.SDK;
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

            listView.ItemSelected += (object sender, SelectedItemChangedEventArgs e) =>
            {
                if (e.SelectedItem == null)
                {
                    return; // don't do anything if we just de-selected the row
                }
                // do something with e.SelectedItem
                ((ListView)sender).SelectedItem = null; // de-select the row
            };
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

        public void refresh()
        {
            App.cvm.GetView();
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
                    App.cvm.deleteCart();
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
                App.cvm.deleteCart();
            }

        }
    }  
}
