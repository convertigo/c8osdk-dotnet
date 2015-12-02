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
    public partial class SubClass : ContentPage
    {
        //Private string object that we recieve in parameters of SubClass
        private string category;

        public SubClass(string category)
        {
            InitializeComponent();
            Category = category;

        }

        public string Category
        {
            get
            {
                return category;
            }

            set
            {
                category = value;
            }
        }

        protected override void OnAppearing()
        {
            /*App.myC8o.Call("fs://.view",
                new Dictionary<string, object>
                {

                    {"ddoc", "design"},
                    {"view", "view"},
                    {"startkey" , "['42','Menu']"},
                    {"endkey", "['42','Menu',{}]"},
                    {"limit", 10},
                    {"skip", 0}

                },
                new C8oJsonResponseListener((jsonResponse, parameters) =>
                {
                    App.myC8o.Log(C8oLogLevel.DEBUG, jsonResponse.ToString());
                    Object model;
                    App.models.TryGetValue("CategoryViewModel", out model);
                    Model a = (Model)model;
                    a.PopulateData(jsonResponse,true);

                }),

                new C8oExceptionListener((exception, parameters) =>
                {
                    //Debug.WriteLine("Exeption : Message = " + exception.Message + "Fin du message");
                    Debug.WriteLine("Exeption : [ToString] = " + exception.ToString() + "Fin du [ToString]");

                })

            );*/
        }


    }
}
