using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK.Listeners;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Windows;

namespace Sample01WindowsPhone
{
    class C8oXMLResponseListenerImp : C8oXmlResponseListener
    {
        private TextBlock textBlock;

        public C8oXMLResponseListenerImp(TextBlock textBlock)
        {
            this.textBlock = textBlock;
        }

        public void onXmlResponse(XDocument xmlResponse, Dictionary<String, Object> parameters)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                textBlock.Text = xmlResponse.ToString();
            });
        }

    }
}