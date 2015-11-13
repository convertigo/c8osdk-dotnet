using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Convertigo.SDK.Listeners;
using Convertigo.SDK;

namespace MobileFormsDemo
{
    public partial class ListPage : ContentPage
    {
        public ListPage()
        {
            InitializeComponent();
        }

        public void OnSyncClicked(object sender, EventArgs args)
        {
            App.myC8o.Call("fs://.sync",
                null,
                new C8oJsonResponseListener((jsonResponse, parameters) =>
                {
                    Object model;
                    App.models.TryGetValue("SyncModel", out model);
                    ((Model)model).PopulateData(jsonResponse);
                    App.myC8o.Log(C8oLogLevel.DEBUG, jsonResponse.ToString());
                })
            );
        }

        public void OnListClicked(object sender, EventArgs args)
        {
            App.myC8o.Call("fs://.view",
                new Dictionary<string, object>
                {
                    {"ddoc", "SFDesign"},
                    {"view", "Objects"},
                    {"startkey", "'Contact'"},
                    {"endkey", "'ContactZ'"} /*,
                    {"limit", 3},
                    {"skip", 5}*/
                },
                new C8oJsonResponseListener((jsonResponse, parameters) =>
                {
                    Object model;
                    App.models.TryGetValue("ListViewModel", out model);
                    ((Model)model).PopulateData(jsonResponse);
                })
            );
        }
    }
}
