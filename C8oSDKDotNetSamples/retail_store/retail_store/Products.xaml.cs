﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

using Xamarin.Forms;


namespace retail_store
{
    public partial class Products : ContentPage
    {
        public Products()
        {
            InitializeComponent();
            IsVisibleProd(false);
            NavigationPage.SetHasNavigationBar(this, false);
            listView.SeparatorColor = Color.Black;
            
            }
        
        public async  void tapImage_Tapped_promo(object sender, EventArgs e)
        {
            if (listView.IsVisible == false)
            {
                if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
                {
                    ((CategoryTablet)((TabbedPageP)Parent.Parent).tabletP).ListPage.Add(this);
                    ((CategoryTablet)((TabbedPageP)Parent.Parent).tabletP).Master = new Category("PROMO");
                    ((TabbedPageP)Parent.Parent).CurrentPage = ((TabbedPageP)Parent.Parent).Children[1];
                }
                else
                {
                    Category c = new Category("PROMO");
                    await Navigation.PushAsync(c, true);
                }   
            }
            
        }
        public async void tapImage_Tapped(object sender, EventArgs e)
        {
            if (listView.IsVisible == false)
            {
                if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
                {
                    ((CategoryTablet)((TabbedPageP)Parent.Parent).tabletP).ListPage.Add(this);
                    ((CategoryTablet)((TabbedPageP)Parent.Parent).tabletP).Master = new Category("NEWS");
                    ((TabbedPageP)Parent.Parent).CurrentPage = ((TabbedPageP)Parent.Parent).Children[1];
                }
                else
                {
                    Category c = new Category("NEWS");
                    await Navigation.PushAsync(c, true);
                }
                
            }
        }

        public void OnSearch(object sender, EventArgs e)
        {
            if (SearchFor.Text != "")
            {
                IsVisibleProd(true);
                string val = SearchFor.Text;
                string valUp = val.ToUpper();
                string valLow = val.ToLower();
                Search(valLow, valUp);
            }
            else
            {
                IsVisibleProd(false);
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        public async void Search(string valLow, string valUp)
        {
            JObject data = await App.myC8o.CallJson(
                   "fs://.view",
                   "ddoc", "design",
                   "view", "Search",
                   "startkey", "['42', '" + valLow + "']",
                   "endkey", "['42', '" + valUp + "Z']",
                   "limit", 20,
                   "skip", 0)
                   .Progress(progress =>
                   {
                       Debug.WriteLine(progress.TaskInfo);
                   })
                   .Fail((e, p) =>
                   {
                       Debug.WriteLine("LAA" + e);// Handle errors..
                   })
                   .Async();
            Object model;
            App.models.TryGetValue("CategoryViewModel", out model);
            Model a = (Model)model;
           
            a.PopulateData(data, true);
            listView.BindingContext = a;

        }
        public void IsVisibleProd(bool b)
        {
            double val;
            if (b)
            {
                val = 0.15;
            }
            else
            {
                val = 1;

            }
            listView.IsVisible = b;
            imgN.Opacity = val;
            imgP.Opacity = val;
            imgN_text.Opacity = val;
            imgP_text.Opacity = val;
            img_fresh.Opacity = val;
            //txt_ret.Opacity = val;
            txt_fr.Opacity = val;
        }

        async void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            Prod prod = (Prod)e.Item;
            await Navigation.PushAsync(new Detail(prod), true);
        }
     }
}
