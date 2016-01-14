using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Convertigo.SDK;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;

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
        private bool isRight;
        public Action<Detail> ItemSelected { get; set; }
        
        //Constructor by default here category is set to "Menu" it's the root category of our Design document.
        //isProduct is also set to false by default because our first root menu is not a product. 
        public Category(string category = "Menu",  Boolean isProduct = false, string category2 = "Menu", string view= "children_byFather")
        {
            //Title = "return";
            InitializeComponent();
            if (Device.OS == TargetPlatform.iOS)
            {
                if (category == "Menu")
                {
                    NavigationPage.SetHasNavigationBar(this, false);
                }
                else
                {
                    NavigationPage.SetHasNavigationBar(this, true);
                }
            }
            else
            {
                NavigationPage.SetHasNavigationBar(this, false);
            }
            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
                this.Img.IsVisible = false;
                this.stack.IsVisible = false;
            }

            this.IsRight = isRight;
            this.Categor = category;
            this.Categor2 = category2;
            this.isProduct = isProduct;
            this.view = view;
            IsSearch = isSearch;
            this.listView.SeparatorColor = Color.Black;
            this.Categor2 = category;
            this.Leaf = "";
            this.Leaf2 = "{}";
            run();
          
            
        }

        public async void run()
        {
            this.indicator.IsVisible = true;
            this.indicatorStr.IsVisible = true;
            //Here we call, thanks to myC8o object, a new view on our local base with specified parameters.
            JObject data = await App.myC8o.CallJson(
                    "fs://.view",                                   //We get here a view from the default project as the project has been define in the endpoint URL.  
                    "ddoc", "design",                               //And give here parameters
                    "view", view,
                    "startkey", "['42','" + Categor + "'," + Leaf + "]",
                    "endkey", "['42','" + Categor2 + "'," + Leaf2 + "]",
                    "skip", 0)
                    .Fail((e, p) =>
                    {
                        Debug.WriteLine("" + e);                    // Handle errors..
                    })
                    .Async();                                       //Async Call

            indicator.IsVisible = false;
            indicatorStr.IsVisible = false;
            Object model;
            App.models.TryGetValue("CategoryViewModel", out model);
            Model mod = (Model)model;
            mod.PopulateData(data, IsProduct);
            //imageB.Source = ((CategoryViewModel)mod).Rayons.
            /*if (isProduct)
            {
              foreach(Rayon r in ((CategoryViewModel)mod).Rayons)
                {
                    JObject data2 = await App.myC8o.CallJson(
                        "fs://.get",               //We post here the an item into the cart from the default project as the project has been define in the endpoint URL. 
                        "docid", r.Id           //And give here parameters
                        )
                        .Fail((e, q) =>
                        {
                            Debug.WriteLine("" + e); // Handle errors..
                        })
                        .Async();                   //Async Call
                    if (data2["_attachments"]["img.jpg"]["content_path"] != null)
                    {
                        byte[] b = DependencyService.Get<IGetImage>().GetMyImage(((string)data2["_attachments"]["img.jpg"]["content_path"]));
                        r.Img = ImageSource.FromStream(() => new MemoryStream(b));
                    }
                }
                //this.imageB.
            }*/

        }

        public async void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
                //If the item is not product we push async a new category view
                if (IsProduct == false)
                {
                    //((CategoryTablet)Parent).ListPage.Add(this);
                    Categ categ = (Categ)e.Item;
                    await Navigation.PushAsync(new Category(categ.LevelId, categ.Leaf), true);
                    //((CategoryTablet)Parent).Master = new Category(categ.LevelId, categ.Leaf);
                }
                //If the item is a product we push async a new detail view
                else
                {
                    Prod prod = (Prod)e.Item;
                    ((CategoryTablet)Parent.Parent).Detail = new Detail(prod);
                    listView.SelectedItem = 0;

                }
            }
            else
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

        public bool IsRight
        {
            get
            {
                return isRight;
            }

            set
            {
                isRight = value;
            }
        }
    }
}
