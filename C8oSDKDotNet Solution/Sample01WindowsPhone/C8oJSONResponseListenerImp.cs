using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK.Listeners;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using System.Windows;

namespace Sample01WindowsPhone
{
    class C8oJSONResponseListenerImp : C8oJsonResponseListener
    {
        private TextBlock textBlock;

        public C8oJSONResponseListenerImp(TextBlock textBlock)
            : base((jsonResponse, parameters) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    textBlock.Text = jsonResponse.ToString();
                });
            })
        {
            this.textBlock = textBlock;
        }
    }
}
