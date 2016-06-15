using System;
using System.Diagnostics;

using Xamarin.Forms;

namespace SampleBigFileTransfer
{
    public partial class Login : ContentPage
    {
        App app;

        public Login (App app)
		{
			InitializeComponent();

            this.app = app;
        }
        
        private async void LoginButtonClick(Object sender, EventArgs args)
        {
            string username = UsernameField.Text;

            var loginResponse = await app.c8o.CallJson(".Login",
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
