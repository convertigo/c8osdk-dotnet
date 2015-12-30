using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace retail_store
{
    class CategoryViewModel : INotifyPropertyChanged, Model
    {
        
        List<Rayon> rayons;

        public event PropertyChangedEventHandler PropertyChanged;

        public CategoryViewModel()
        {
            // regsiter this model in the App opbject so it can be retreived from anywhere..
            App.models.Remove("CategoryViewModel");
            App.models.Add("CategoryViewModel", this);
            rayons = new List<Rayon>();
        }

        public List<Rayon> Rayons
        {
            set
            {
                rayons = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
                
            }
            get
            {
                return rayons;
            }
        }
        public void PopulateData(JObject json, Boolean isproduct)
        {
            if (isproduct == false)
            {
                List<Rayon> data = new List<Rayon>();
                foreach (JObject jo in (JArray)json["rows"])
                {
                    data.Add(new Categ((String)jo["value"]["name"], (String)jo["value"]["imageUrl"], (String)jo["id"], (String)jo["key"][0], (String)jo["key"][1],(String)jo["value"]["levelId"], (String)jo["value"]["leaf"]));
                }
                this.Rayons = data;
            }
            else
            {
                List<Rayon> data = new List<Rayon>();
                foreach (JObject jo in (JArray)json["rows"])
                {
                    data.Add(new Prod((String)jo["value"]["name"], (String)jo["value"]["imageUrl"], (String)jo["id"], (String)jo["key"][0], (String)jo["key"][1], (String)jo["value"]["sku"], (String)jo["value"]["priceOfUnit"]));
                }
                this.Rayons = data;
            }
        }




    }
}

