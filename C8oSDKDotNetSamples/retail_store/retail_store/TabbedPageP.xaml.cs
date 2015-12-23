using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace retail_store
{
    public partial class TabbedPageP : TabbedPage
    {
        public NavigationPage pr;
        public NavigationPage np;
        public Cart myCart;
        public Settings  Setting;
        public TabbedPageP()
        {
            InitializeComponent();
            
            pr = new NavigationPage(new Products()) { Title = "Products"};
            np = new NavigationPage(new Category()) { Title = "Category"};
            myCart = new Cart() { Title = "Cart"};
            Setting = new Settings() { Title = "Settings" };




            np.BackgroundColor = Color.FromHex("#FFFFFF");
            this.Children.Add(pr);
            this.Children.Add(np);
            this.Children.Add(myCart);
            this.Children.Add(Setting);
        }
        
    }
}
