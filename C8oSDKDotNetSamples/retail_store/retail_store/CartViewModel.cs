﻿using System;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using System.IO;
using System.ComponentModel;

namespace retail_store
{
    public class CartViewModel : INotifyPropertyChanged, Model
    {
        //list of productstock contains in local base
        private ObservableCollection<ProdStock> productStock;
        private ObservableCollection<ReduceTot> reduce;
        //this is the product that we wants to insert.
        private Prod product;
        //this is the product stock that we will insert into database;
        public event PropertyChangedEventHandler PropertyChanged;
        private ProdStock productToInsert;

        //Constructors
        public CartViewModel(Prod product)
        {
            this.Product = product;
            App.models.Remove("CartViewModel");
            App.models.Add("CartViewModel", this);
            ProductStock = new ObservableCollection<ProdStock>();
            Reduce = new ObservableCollection<ReduceTot>();
            Reduce.Add(new ReduceTot("0", "0"));
        }
        public CartViewModel()
        {
            App.models.Remove("CartViewModel");
            App.models.Add("CartViewModel", this);
            ProductStock = new ObservableCollection<ProdStock>();
            Reduce = new ObservableCollection<ReduceTot>();
            Reduce.Add(new ReduceTot("0", "0"));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }
        //Getters and Setters
        public ObservableCollection<ProdStock> ProductStock
        {
            get
            {
                return productStock;
            }
            set
            { 
                productStock = value;
                OnPropertyChanged("ProductStock");
            }
        }

        public Prod Product
        {
            get
            {
                return product;
            }

            set
            {
                product = value;
            }
        }

        public ObservableCollection<ReduceTot> Reduce
        {
            get
            {
                return reduce;
            }

            set
            {
                reduce = value;
            }
        }


        //PopulateData allow us to get the whole objects contained in the local base and put it on productStock(list)
        public async void PopulateData(JObject json,bool check)
        {
            ProductStock.Clear();
            ObservableCollection<ProdStock> data = new ObservableCollection<ProdStock>();
            foreach (JObject jo in (JArray)json["rows"])
            {
                JObject data2 = await App.myC8o.CallJson(
                        "fs://.get",               //We post here the an item into the cart from the default project as the project has been define in the endpoint URL. 
                        "docid", (String)jo["id"]          //And give here parameters
                        )
                        .Fail((e, q) =>
                        {
                            Debug.WriteLine("" + e); // Handle errors..
                        })
                        .Async();
                try
                {
                    if (data2["_attachments"]["img.jpg"]["content_path"] != null)
                    {
                        byte[] b = DependencyService.Get<IGetImage>().GetMyImage(((string)data2["_attachments"]["img.jpg"]["content_path"]));
                        ImageSource i = ImageSource.FromStream(() => new MemoryStream(b));

                        ProductStock.Add(new ProdStock((String)jo["value"]["name"], (String)jo["value"]["imageUrl"], (String)jo["id"], (String)jo["value"]["shopcode"], (String)jo["value"]["fatherId"], (String)jo["value"]["sku"], (String)jo["value"]["priceOfUnit"], (float)jo["value"]["count"], i));
                    }
                    else
                    {

                        ProductStock.Add( new ProdStock((String)jo["value"]["name"], (String)jo["value"]["imageUrl"], (String)jo["id"], (String)jo["value"]["shopcode"], (String)jo["value"]["fatherId"], (String)jo["value"]["sku"], (String)jo["value"]["priceOfUnit"], (float)jo["value"]["count"]));
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
        }

       
        //CheckCart allow us to check if the products that we want to insert is already contains in database 
        public void CheckCart(bool plus)
        {
            bool flag = false;
            bool del = false;

            foreach(ProdStock pStock in this.ProductStock)
            {
                //If we found the produce we change count attributes to update the amount
                if (pStock.Id == Product.Id)
                {
                    flag = true;
                    productToInsert = pStock;
                    //if we want to add a product
                    if (plus)
                    {
                        productToInsert.Count += 1;
                    }
                    else
                    {
                        productToInsert.Count -= 1;
                        if (productToInsert.Count ==0)
                        {
                            del = true;
                            goto goOut;
                        }
                    }
                    updateCart();
                    goto goOut;
                }
            }
            goOut:
            //Else if flag is set to false we create a new ProdStock object to add it on database.
            if (flag==false)
            {
                productToInsert = new ProdStock(Product.Name,Product.ImageUrl,Product.Id,Product.Shopcode,Product.FatherId,Product.Sku,Product.PriceOfUnit, 1, Product.Imgs);
                ProductStock.Add(productToInsert);
                insertCart();
            }
            if (del)
            {
                deleteCart();
            }
            
        }

        public async void deleteCart()
        {
            foreach (ProdStock pStock in ProductStock)
            {
                if (pStock.Id == product.Id)
                {
                    await App.myC8oCart.CallJson(
                        "fs://.post",               //We post here the an item into the cart from the default project as the project has been define in the endpoint URL. 
                        "_id", pStock.Id,           //And give here parameters
                        "count", 0,
                        "_use_policy", "merge")     
                        .Fail((e, q) =>
                        {
                            Debug.WriteLine(""+e); // Handle errors..
                    })
                        .Async();                   //Async Call

                    await App.myC8oCart.CallJson(
                        "fs://.delete",             //We delete here the an item into the cart from the default project as the project has been define in the endpoint URL. 
                        "docid", pStock.Id)         //And give here parameters
                        .Fail((e, q) =>
                        {
                            Debug.WriteLine(e.ToString()); // Handle errors..
                    })
                    .Async();                             //Async Call

                    ProductStock.Remove(pStock);         //We remove this item from our array of itm
                    break;
                }   
            }
            App.cvm.GetReducePrice();               // We update our reduce price
        }


        public async void updateCart()
        {
            await App.myC8oCart.CallJson(
                "fs://.post",                   //We post here the an item into the cart from the default project as the project has been define in the endpoint URL. 
                "_id", productToInsert.Id,      //And give here parameters
                "count", productToInsert.Count,
                "_use_policy", "merge")
                .Fail((e, q) =>
                {
                    Debug.WriteLine(e.ToString()); // Handle errors..
                })
                .Async();                         //Async Call

            App.cvm.GetReducePrice();             // We update our reduce price
        }

        //insertCart as indicated by his name insert a new productStock in local base
        public async void insertCart()
        {
            //We verify if productToInsert as weel been assigned.
            if (productToInsert != null)
            {
                await App.myC8oCart.CallJson(
                    "fs://.post",                                       //We post here the an item into the cart from the default project as the project has been define in the endpoint URL.
                    "_id", productToInsert.Id,                         //And give here parameters
                    "name", productToInsert.Name,
                    "imageUrl", productToInsert.ImageUrl,
                    "count", productToInsert.Count,
                    "priceOfUnit",productToInsert.PriceOfUnit,
                    "sku", productToInsert.Sku,
                    "shopcode",productToInsert.Shopcode,
                    "fatherId",productToInsert.FatherId)
                    .Fail((e, q) =>
                    {
                        Debug.WriteLine(e.ToString());  // Handle errors..
                    })
                .Async();                               //Async Call

                App.cvm.GetReducePrice();              // We update our reduce price
            }
            
        }

        public async void GetReducePrice()
        {
            JObject data = await App.myC8oCart.CallJson(
                    "fs://.view",                            //We get here a view from the default project as the project has been define in the endpoint URL.
                    "ddoc", "design",                       //And give here parameters
                    "view", "reduceTot"
                    )
                    .Fail((e, p) =>
                    {
                        Debug.WriteLine(e.ToString()); // Handle errors..
                    })
                    .Async();                          //Async Call

            SetReduce(data);
        }

        public void SetReduce(JObject jsRep)
        {
            Boolean flag = false;
            foreach (JObject jo in (JArray)jsRep["rows"])
            {
                //Browsing JArray in order to collect data
                if (((string)jo["value"]["total"]).ToString() != null)
                {
                    this.Reduce[0].Total = ((string)jo["value"]["total"]).ToString();
                    this.Reduce[0].Count = ((string)jo["value"]["count"]).ToString();
                    flag = true;
                }   
            }
            if (!flag)
            {
                this.Reduce[0].Total = "0";
                this.Reduce[0].Count = "0";
            }
        }

        public void SetProductBySku(string id)
        {
            foreach (ProdStock p in productStock)
            {
                if (p.Id == id)
                {
                    Product = p;
                    break;
                }
            }
        }

        public async void GetRealPrice()
        {
            JObject data = await App.myC8oCart.CallJson(
                    "fs://.view",                            //We get here a view from the default project as the project has been define in the endpoint URL.     
                    "ddoc", "design",                        //And give here parameters
                    "view", "CartPrice")
                    .Fail((e, p) =>
                    {
                        Debug.WriteLine("" + e);        // Handle errors..
                    })
                    .Async();                           //Async Call

            SetRealPrice(data);
        }

        public void SetRealPrice(JObject jsonResponse)
        {    
             Boolean flag = false;
            //Browsing JArray in order to collect data
            foreach (JObject jo in (JArray)jsonResponse["rows"])
             {
                if (Math.Round(((float)jo["value"]["newPrice"]), 2).ToString() != this.Reduce[0].NewPrice)
                {
                    this.Reduce[0].Discount = (((string)jo["value"]["discount"])).ToString();
                    this.Reduce[0].NewPrice = Math.Round(((float)jo["value"]["newPrice"]), 2).ToString();
                    GetView();
                }
                 flag = true;
             }
             if (!flag)
             {
                 this.Reduce[0].Discount = "0";
                 this.Reduce[0].NewPrice = "0";
             }
        }

        public async void GetView()
        {
            JObject data = await App.myC8oCart.CallJson(
                "fs://.view",                               //We get here a view from the default project as the project has been define in the endpoint URL.
                "ddoc", "design",                           //And give here parameters
                "view", "view")
                .Fail((e, p) =>
                {
                    Debug.WriteLine("" + e);        // Handle errors..
                })
                .Async();                           //Async Call

            PopulateData(data, true);
        }
    }
}
