using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK.Listeners;
using Xamarin.Forms;
using Newtonsoft.Json.Linq;

namespace Sample01XamarinForms
{
    class C8oJSONResponseListenerImp : C8oJsonResponseListener
    {
        private Label label;

        public C8oJSONResponseListenerImp(Label label)
            : base((jsonResponse, parameters) =>
            {
                label.Text = jsonResponse.ToString();
            })
        {
            this.label = label;
        }
    }
}
