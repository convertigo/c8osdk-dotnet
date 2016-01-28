using System;
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
                    ((TabbedPageP)Parent.Parent).CurrentPage = ((TabbedPageP)Parent.Parent).Children[1];
                    await ((CategoryTablet)((TabbedPageP)Parent.Parent).tabletP).Master.Navigation.PushAsync(new Category("PROMO"));
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
                    ((TabbedPageP)Parent.Parent).CurrentPage = ((TabbedPageP)Parent.Parent).Children[1];
                    await ((CategoryTablet)((TabbedPageP)Parent.Parent).tabletP).Master.Navigation.PushAsync(new Category("NEWS"));
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
            listView.IsRefreshing = false;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        public async void Search(string valLow, string valUp)
        {
           
            JObject data = await App.myC8o.CallJson(
                   "fs://.view",                                    //We get here a view from the default project as the project has been define in the endpoint URL.   
                   "ddoc", "design",                                //And give here parameters
                   "view", "Search",
                   "startkey", "['42', '" + valLow + "']",
                   "endkey", "['42', '" + valUp + "Z']",
                   //"limit", 20,
                   "skip", 0)
                   .Fail((e, p) =>
                   {
                       Debug.WriteLine("LAA" + e);                // Handle errors..
                   })
                   .Async();                                      //Async Call
            
            CategoryViewModel categvm = new CategoryViewModel();
            categvm.PopulateData(data, true);
            listView.BindingContext = categvm;

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
            listView.IsRefreshing = true;
            imgN.Opacity = val;
            imgP.Opacity = val;
            imgN_text.Opacity = val;
            imgP_text.Opacity = val;
            img_fresh.Opacity = val;
            txt_fr.Opacity = val;
        }

        async void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            Prod prod = (Prod)e.Item;
            await Navigation.PushAsync(new Detail(prod), true);
        }
     }
}
