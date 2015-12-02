using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Convertigo.SDK;
using Convertigo.SDK.Listeners;
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

        //Constructor by default here category is set to "Menu" it's the root category of our Design document.
        //isProduct is also set to false by default because our first root menu is not a product. 
        public Category(string category = "Menu", Boolean isProduct = false)
        {
            InitializeComponent();
            this.Categor = category;
            this.isProduct = isProduct;
            this.view = "children_byFather";
            
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

        async void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            //If the item is not product we push async a new category view
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

        protected override void OnAppearing()
        {
            //Here we call, thanks to myC8o object, a new view on our local base with specified parameters.
            App.myC8o.Call("fs://.view",
                new Dictionary<string, object>
                {
                    {"ddoc", "design"},
                    {"view", view},
                    {"startkey" , "['42','"+Categor+"']"},
                    {"endkey", "['42','"+Categor+"',{}]"},
                    {"limit", 20},
                    {"skip", 0}

                },
                new C8oJsonResponseListener((jsonResponse, parameters) =>
                {
                    Debug.WriteLine(jsonResponse.ToString());
                    App.myC8o.Log(C8oLogLevel.DEBUG, jsonResponse.ToString());
                    Object model;
                    App.models.TryGetValue("CategoryViewModel", out model);
                    Model a = (Model)model;
                    a.PopulateData(jsonResponse, IsProduct);

                }),

                new C8oExceptionListener((exception, parameters) =>
                {
                    Debug.WriteLine("Exeption : [ToString] = " + exception.ToString() + "Fin du [ToString]");
                })
            );

        }
        
    }
}
