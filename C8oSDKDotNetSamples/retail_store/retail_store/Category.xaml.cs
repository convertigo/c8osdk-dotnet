using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Convertigo.SDK;
//using Convertigo.SDK.Listeners;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace retail_store
{
    public partial class Category : ContentPage
    {
        //Private flieds.
        private string categor;
        private Boolean isProduct;
        private String view;
        private string categor2;
        private string leaf;
        private string leaf2;
        private Boolean isSearch;

        //Constructor by default here category is set to "Menu" it's the root category of our Design document.
        //isProduct is also set to false by default because our first root menu is not a product. 
        public Category(string category = "Menu",  Boolean isProduct = false, string category2 = "Menu", string view= "children_byFather", Boolean isSearch = false)
        {
            InitializeComponent();
            this.Categor = category;
            this.Categor2 = category2;
            this.isProduct = isProduct;
            this.view = view;
            IsSearch = isSearch;

            if (!isSearch)
            {
                this.Categor2 = category;
                this.Leaf = "";
                this.Leaf2 = "{}";
            }
            else
            {
                this.Leaf = "'true'";
                this.Leaf2 = "'true'";
                IsProduct = true;
            }

            run();
        }

        public async void run()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            this.indicator.IsVisible = true;
            this.indicatorStr.IsVisible = true;
            //Here we call, thanks to myC8o object, a new view on our local base with specified parameters.
            JObject data = await App.myC8o.CallJson(
                    "fs://.view",
                    "ddoc", "design",
                    "view", view,
                    "startkey", "['42','" + Categor + "'," + Leaf + "]",
                    "endkey", "['42','" + Categor2 + "'," + Leaf2 + "]",
                    "limit", 20,
                    "skip", 0)
                    .Fail((e, p) =>
                    {
                        Debug.WriteLine("LAA" + e);// Handle errors..
                    })
                    .Async();
            indicator.IsVisible = false;
            indicatorStr.IsVisible = false;
            Object model;
            App.models.TryGetValue("CategoryViewModel", out model);
            Model mod = (Model)model;
            mod.PopulateData(data, IsProduct);
        }
        //Getters and Setters
        public string Categor
        {
            get
            {
                return categor;
            }

            set
            {
                categor = value;
            }
        }


        public bool IsProduct
        {
            get
            {
                return isProduct;
            }

            set
            {
                isProduct = value;
            }
        }

        public string Categor2
        {
            get
            {
                return categor2;
            }

            set
            {
                categor2 = value;
            }
        }

        public string Leaf
        {
            get
            {
                return leaf;
            }

            set
            {
                leaf = value;
            }
        }

        public string Leaf2
        {
            get
            {
                return leaf2;
            }

            set
            {
                leaf2 = value;
            }
        }

        public bool IsSearch
        {
            get
            {
                return isSearch;
            }

            set
            {
                isSearch = value;
            }
        }

        async void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (IsSearch)
            {
                Prod prod = (Prod)e.Item;
                await Navigation.PushAsync(new Detail(prod), true);
            }
            else
            {             //If the item is not product we push async a new category view
                if (IsProduct == false)
                {
                    Categ categ = (Categ)e.Item;
                    await Navigation.PushAsync(new Category(categ.LevelId, categ.Leaf), true);
                }
                //If the item is a product we push async a new detail view
                else
                {
                    Prod prod = (Prod)e.Item;
                    await Navigation.PushAsync(new Detail(prod), true);
                }
            }
            
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
        }

        
    }
}
