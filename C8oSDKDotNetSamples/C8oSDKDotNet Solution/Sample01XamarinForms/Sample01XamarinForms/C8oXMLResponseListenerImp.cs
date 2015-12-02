using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Convertigo.SDK.Listeners;
using Xamarin.Forms;

namespace Sample01XamarinForms
{
    class C8oXMLResponseListenerImp : C8oXmlResponseListener
    {
        private Label label;

        public C8oXMLResponseListenerImp(Label label)
            : base()
        {
            this.label = label;
        }

        public void onXmlResponse(XDocument xmlResponse, Dictionary<String, Object> parameters)
        {
            //Device.BeginInvokeOnMainThread(() =>
            //{
            label.Text = xmlResponse.ToString();
            //});
        }
    }
}
