using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
////using Convertigo.SDK.Listeners;
using Convertigo.SDK;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace retail_store
{
    public class CartViewModel : ObservableCollection<ProdStock>, Model
    {
        //public event PropertyChangedEventHandler PropertyChanged;
        
        //list of productstock contains in local base
        private ObservableCollection<ProdStock> productStock;
        private ObservableCollection<ReduceTot> reduce;
        //this is the product that we wants to insert.
        private Prod product;
        //this is the product stock that we will insert into database;
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
        public void PopulateData(JObject json,bool check)
        {
            ObservableCollection<ProdStock> data = new ObservableCollection<ProdStock>();
            foreach (JObject jo in (JArray)json["rows"])
            {
                data.Add(new ProdStock((String)jo["value"]["name"], (String)jo["value"]["imageUrl"], (String)jo["id"], (String)jo["value"]["shopcode"], (String)jo["value"]["fatherId"],(String)jo["value"]["sku"], (String)jo["value"]["priceOfUnit"],(float)jo["value"]["count"]));
            }
            if (data !=null)
            {
                this.ProductStock = data;
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

                if (pStock.Sku == Product.Sku)
                {
                    flag = true;
                    productToInsert = pStock;
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
                            ProductStock.Remove(productToInsert);
                            
                        }
                    }
                    updateCart();

                    goto goOut;
                }
            }
            goOut:
            //Else we create a new ProdStock object to add it on database.
            if (flag==false)
            {
                productToInsert = new ProdStock(Product.Name,Product.ImageUrl,Product.Id,Product.Shopcode,Product.FatherId,Product.Sku,Product.PriceOfUnit, 1);
                ProductStock.Add(productToInsert);
                insertCart();
            }
            if (del)
            {
                deleteCart();
            }
            
        }

        public async void deleteCart(Boolean all=false)
        {
            if (all)
            {
                foreach (ProdStock p in ProductStock)
                {
                    JObject data;
                    data = await App.myC8oCart.CallJson(
                        "fs://.delete",
                        "docid", p.Id)
                        .Fail((e, q) =>
                        {
                            Debug.WriteLine(e.ToString()); // Handle errors..
                        })
                    .Async();   
                }
                ProductStock.Clear();
            }
            else
            {
                JObject data;
                data = await App.myC8oCart.CallJson(
                    "fs://.delete",
                    "docid", productToInsert.Id)
                    .Fail((e, q) =>
                    {
                        Debug.WriteLine(e.ToString()); // Handle errors..
                    })
                    .Async();
            }
        }


        public async void updateCart()
        {
            JObject data;
            data = await App.myC8oCart.CallJson(
                "fs://.post",
                "_id", productToInsert.Id,
                "count", productToInsert.Count,
                "_use_policy","merge")
                .Fail((e, q) =>
                {
                    Debug.WriteLine(e.ToString()); // Handle errors..
                    })
                .Async();
        
        }


        //insertCart as indicated by his name insert a new productStock in local base
        public async void insertCart()
        {
            //We verify if productToInsert as weel been assigned.
            if (productToInsert != null)
            {
                //Then we can insert data into FS://CARTDB thanks to c8o object
                JObject data;
                data = await App.myC8oCart.CallJson(
                        "fs://.post",
                        "_id", productToInsert.Id ,
                        "name", productToInsert.Name,
                        "imageUrl", productToInsert.ImageUrl,
                        "count", productToInsert.Count,
                        "priceOfUnit",productToInsert.PriceOfUnit,
                        "sku", productToInsert.Sku,
                        "shopcode",productToInsert.Shopcode,
                        "fatherId",productToInsert.FatherId)
                        .Fail((e, q) =>
                        {
                            Debug.WriteLine(e.ToString());// Handle errors..
                        })
                    .Async();
            }
            
        }

        public async void GetReducePrice()
        {
            JObject data = await App.myC8oCart.CallJson(
                    "fs://.view",
                    "ddoc", "design",
                    "view", "reduceTot",
                    "reduce", true)
                    .Fail((e, p) =>
                    {
                        Debug.WriteLine(e.ToString()); // Handle errors..
                    })
                    .Async();

            SetReduce(data);
        }

        public void SetReduce(JObject jsRep)
        {
            Boolean flag = false;
            foreach (JObject jo in (JArray)jsRep["rows"])
            {
                this.Reduce[0].Total = ((string)jo["value"]["total"]).ToString();
                this.Reduce[0].Count= ((string)jo["value"]["count"]).ToString();
                flag = true;
            }
            if (!flag)
            {
                this.Reduce[0].Total = "0";
                this.Reduce[0].Count = "0";
            }
            
        }

        public void SetProductBySku(string sku)
        {
            foreach (ProdStock p in productStock)
            {
                if (p.Sku == sku)
                {
                    Product = p;
                    break;
                }
            }

        }

        public async void GetRealPrice()
        {

            JObject data = await App.myC8oCart.CallJson(
                    "fs://.view",
                    "ddoc", "design",
                    "view", "CartPrice")
                    .Fail((e, p) =>
                    {
                        // Handle errors..
                    })
                    .Async();
            SetRealPrice(data);
        }

        public void SetRealPrice(JObject jsonResponse)
        {    
             Boolean flag = false;
             foreach(JObject jo in (JArray)jsonResponse["rows"])
             {
                 this.Reduce[0].Discount = (((string)jo["value"]["discount"])).ToString();
                 this.Reduce[0].NewPrice = ((string)jo["value"]["newPrice"]).ToString();
                 flag = true;
             }
             if (!flag)
             {
                 this.Reduce[0].Discount = "0";
                 this.Reduce[0].NewPrice = "0";
             }
               
        }

    }
}
