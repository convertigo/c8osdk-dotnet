using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
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
            String username = UsernameField.Text;

            XDocument loginResponse = await app.c8o.CallXmlAsync(".Login", new Dictionary<String, Object>
            {
                {"username", username},
                {"password", PasswordField.Password}
            });

            app.OutputArea.Text = loginResponse.ToString();

            XElement authenticated = loginResponse.XPathSelectElement("/document/authenticated");
            if (authenticated != null && authenticated.Value.Equals(username))
            {
                app.ContentArea.Content = new Files(app);
            }
        }
    }
}
