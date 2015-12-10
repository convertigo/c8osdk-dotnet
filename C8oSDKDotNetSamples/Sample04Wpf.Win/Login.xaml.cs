using System.Windows;
using System.Windows.Controls;
using System.Xml.XPath;

namespace Sample04Wpf.Win
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : UserControl
    {
        MainWindow app;

        public Login(MainWindow app)
        {
            InitializeComponent();

            this.app = app;
        }

        async private void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            string username = UsernameField.Text;

            var loginResponse = await app.c8o.CallXml(".Login",
                "username", username,
               "password", PasswordField.Password
            ).Async();

            app.OutputArea.Text = loginResponse.ToString();

            var authenticated = loginResponse.XPathSelectElement("/document/authenticated");
            if (authenticated != null && authenticated.Value.Equals(username))
            {
                app.ContentArea.Content = new Files(app);
            }
        }
    }
}
