using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Convertigo.SDK;

namespace Sample01WindowsPhone
{
    public partial class MainPage : PhoneApplicationPage
    {

        private C8o c8o;
        private C8oJSONResponseListenerImp c8oJSONResponseListenerImp;
        private C8oXMLResponseListenerImp c8oXMLResponseListenerImp;
        private C8oExceptionListenerImpCustom c8oExceptionListenerImpCustom;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // String endpoint = "http://trial.convertigo.net/cems/projects/TestClientSDK/";
            // String endpoint = "https://192.168.100.179:28081/convertigo/projects/TestClientSDK/";
            String endpoint = "http://192.168.100.179:28080/convertigo/projects/TestClientSDK/";
            C8oSettings c8oSettings = new C8oSettings().setTimeout(10000);
            c8oSettings.addCookie("TESTCOOKIENAME", "TESTCOOKIEVALUE");
            this.c8o = new C8o(endpoint, c8oSettings, new C8oExceptionListenerImpDefault());

            this.c8oJSONResponseListenerImp = new C8oJSONResponseListenerImp(Transac1TextBlock);
            this.c8oXMLResponseListenerImp = new C8oXMLResponseListenerImp(Transac2TextBlock);
            this.c8oExceptionListenerImpCustom = new C8oExceptionListenerImpCustom(Transac1TextBlock);
        }

        private void Transac1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            c8o.Call(new Dictionary<string, object>
            {
                {"__connector", "HTTP_connector"},
                {"__transaction", "transac1"},
                {"testVariable", "TEST 01"}
            }, this.c8oJSONResponseListenerImp, this.c8oExceptionListenerImpCustom);
        }

        private void Transac2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            c8o.Call(".HTTP_connector.transac2", new Dictionary<string, Object>
            {
                {"testVariable", "TEST 02"}
            }, this.c8oXMLResponseListenerImp);
        }
    }
}