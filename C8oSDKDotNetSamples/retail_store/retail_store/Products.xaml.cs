using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK;
//using Convertigo.SDK.Listeners;
using System.Diagnostics;

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
            string val = SearchFor.Text;
            string valUp = val.ToUpper();
            string valLow = val.ToLower();
            //int lVal = val.Length;
            Category Categ = new Category(valLow, false, valUp, "Search", true);

            await Navigation.PushAsync(Categ , true);


            
            // ((TabbedPageP)App.MainPage).np
        }
    }
}
