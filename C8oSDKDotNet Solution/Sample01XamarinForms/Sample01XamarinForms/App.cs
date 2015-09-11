using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Convertigo.SDK;

using Xamarin.Forms;

namespace Sample01XamarinForms
{
    public class App : Application
    {
        // C8o objects
        private C8o c8o;
        private C8oJSONResponseListenerImp c8oJSONResponseListenerImp;
        private C8oXMLResponseListenerImp c8oXMLResponseListenerImp;
        private C8oExceptionListenerImpCustom c8oExceptionListenerImpCustom;

        // Components
        private Label label01;
        private Label label02;

        public App()
        {
            // Initialize components
            Button button01 = new Button
            {
                Text = "Transac 1"
            };
            button01.Clicked += OnClickButton01;
            this.label01 = new Label { };
            Button button02 = new Button
            {
                Text = "Transac 2"
            };
            button02.Clicked += OnClickButton02;
            this.label02 = new Label { };

            // Screen
            MainPage = new ContentPage
            {
                Content = new ScrollView
                {
                    Content = new StackLayout
                    {
                        Children = { button01, label01, button02, label02 }
                    }
                }
            };

            // C8o objects
            //String endpoint = "http://trial.convertigo.net.error/cems/projects/TestClientSDK";
            // String endpoint = "https://192.168.100.179:28081/convertigo/projects/TestClientSDK";
            String endpoint = "http://192.168.100.179:28080/convertigo/projects/TestClientSDK";
            C8oSettings c8oSettings = new C8oSettings().setTimeout(1000);
            c8oSettings.addCookie("TESTCOOKIENAME", "TESTCOOKIEVALUE");
            this.c8o = new C8o(endpoint, c8oSettings, new C8oExceptionListenerImpDefault());

            this.c8oJSONResponseListenerImp = new C8oJSONResponseListenerImp(this.label01);
            this.c8oXMLResponseListenerImp = new C8oXMLResponseListenerImp(this.label02);
            this.c8oExceptionListenerImpCustom = new C8oExceptionListenerImpCustom(this.label01);
        }

        void OnClickButton01(object sender, EventArgs ea)
        {
            c8o.Call(new Dictionary<String, Object>
            {
                {"__connector", "HTTP_connector"},
                {"__transaction", "transac1"},
                {"testVariable", "TEST 01"}
            }, this.c8oJSONResponseListenerImp, this.c8oExceptionListenerImpCustom);
        }

        void OnClickButton02(object sender, EventArgs ea)
        {
            c8o.Call(".HTTP_connector.transac2", new Dictionary<String, Object>
            {
                {"testVariable", "TEST 02"}
            }, this.c8oXMLResponseListenerImp);
        }

        protected override void OnStart()
        { }

        protected override void OnSleep()
        { }

        protected override void OnResume()
        { }
    }
}
