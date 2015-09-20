using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using Xamarin.Forms;

using Newtonsoft.Json.Linq;


namespace MobileFormsDemo
{
    // This is the model for our listView. Hold a Customer Object list
    class ListViewModel : INotifyPropertyChanged, Model
    {
        public class Customer
        {
            String name;
            String address;

            public Customer(String name, String address)
            {
                this.name = name;
                this.address = address;
            }

            public String Name
            {
                get
                {
                    return name;
                }
            }

            public String Address
            {
                get
                {
                    return address;
                }
            }

        };

        List<Customer> customers;

        public event PropertyChangedEventHandler PropertyChanged;

        public ListViewModel ()
        {
            // regsiter this model in the App opbject so it can be retreived from anywhere..
            App.models.Add("ListViewModel", this);
            customers = new List<Customer>();
        }

        public List<Customer> Customers
        {
            set
            {
                customers = value;
                PropertyChanged(this, new PropertyChangedEventArgs(null));
            }
            get
            {
                return customers;
            }
        }

        public void PopulateData(JObject json)
        {
            List<Customer> data = new List<Customer>();
            foreach (JObject jo in (JArray)json["rows"])
            {
                data.Add(new Customer((String)jo.SelectToken("value.name"), (String)jo.SelectToken("value.id")));
            }
            this.Customers = data;
        }
    }
}
