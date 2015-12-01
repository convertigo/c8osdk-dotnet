using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xamarin.Forms;
using Newtonsoft.Json.Linq;
using Convertigo.SDK;

namespace Sample04XamarinForms
{
    public partial class Login : ContentPage
    {
        App app;

        public Login(App app)
        {
            InitializeComponent();

            this.app = app;
        }

        async private void LoginButtonClick(Object sender, EventArgs args)
        {
            String username = UsernameField.Text;

            JObject loginResponse = await app.c8o.CallJson(".Login", 
                "username", username,
                "password", PasswordField.Text
            ).Async();

            Debug.WriteLine(loginResponse.ToString());
            
            if (loginResponse.SelectToken("document.authenticated").ToString().Equals(username))
            {
                app.MainPage = new Files(app);
            }
        }
    }
}
