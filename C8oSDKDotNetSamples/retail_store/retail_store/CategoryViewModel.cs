using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Xamarin.Forms;
using System.IO;

namespace retail_store
{
    public class CategoryViewModel : ObservableCollection<Rayon>, Model
    {
        ObservableCollection<Rayon> rayons;
        

        public CategoryViewModel()
        {
            // regsiter this model in the App opbject so it can be retreived from anywhere..
            App.models.Remove("CategoryViewModel");
            App.models.Add("CategoryViewModel", this);
            rayons = new ObservableCollection<Rayon>();
        }
        //Getters and Setters
        public ObservableCollection<Rayon> Rayons
        {
            set
            {
                rayons = value;
                
            }
            get
            {
                return rayons;
            }
        }

        public async void PopulateData(JObject json, Boolean isproduct)
        {
            if (isproduct == false)
            {
                foreach (JObject jo in (JArray)json["rows"])
                {
                    Rayons.Add(new Categ((String)jo["value"]["name"], (String)jo["value"]["imageUrl"], (String)jo["id"], (String)jo["key"][0], (String)jo["key"][1],(String)jo["value"]["levelId"], (String)jo["value"]["leaf"]));
                }
            }
            else
            { 
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
                            Rayons.Add(new Prod((String)jo["value"]["name"], (String)jo["value"]["imageUrl"], (String)jo["id"], (String)jo["key"][0], (String)jo["key"][1], (String)jo["value"]["sku"], (String)jo["value"]["priceOfUnit"], i));
                        }
                        else
                        {
                            Rayons.Add(new Prod((String)jo["value"]["name"], (String)jo["value"]["imageUrl"], (String)jo["id"], (String)jo["key"][0], (String)jo["key"][1], (String)jo["value"]["sku"], (String)jo["value"]["priceOfUnit"]));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                    
                }
                //this.Rayons = data;
            }
        }




    }
}

