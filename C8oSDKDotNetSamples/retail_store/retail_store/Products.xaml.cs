using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK;
//using Convertigo.SDK.Listeners;
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
            
            //Creating TapGestureRecognizers  
            var tapImage = new TapGestureRecognizer();
            //Binding events  
            tapImage.Tapped += tapImage_Tapped;
            //Associating tap events to the image buttons  
            imgN.GestureRecognizers.Add(tapImage);
            imgP.GestureRecognizers.Add(tapImage);
            
            
        }
        void tapImage_Tapped(object sender, EventArgs e)
        {
            // handle the tap  
            DisplayAlert("Alert", "This is an image button", "OK");
        }

        public async void OnSearch(object sender, EventArgs e)
        {
            if (SearchFor.Text != "")
            {
                listView.IsVisible = true;
            }
            else
            {
                listView.IsVisible = false;
            }
            string val = SearchFor.Text;
            string valUp = val.ToUpper();
            string valLow = val.ToLower();
            //int lVal = val.Length;
            Search(valLow, valUp);


            // ((TabbedPageP)App.MainPage).np
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        public async void Search(string valLow, string valUp)
        {
            JObject data = await App.myC8o.CallJson(
                   "fs://.view",
                   "ddoc", "design",
                   "view", "Search",
                   "startkey", "['42', '" + valLow + "]",
                   "endkey", "['42', '" + valUp + "Z']",
                   "limit", 20,
                   "skip", 0)
                   .Fail((e, p) =>
                   {
                       Debug.WriteLine("LAA" + e);// Handle errors..
                   })
                   .Async();
            Object model;
            App.models.TryGetValue("CategoryViewModel", out model);
            Model a = (Model)model;
            a.PopulateData(data, true);

        }
    }
}
