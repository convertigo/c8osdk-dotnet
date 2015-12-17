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
        public TabbedPageP()
        {
            InitializeComponent();
            pr = new NavigationPage(new Products()) { Title = "Products" };
            NavigationPage.SetHasNavigationBar(pr, false);
            np = new NavigationPage(new Category()) { Title = "Category" };
            NavigationPage.SetHasNavigationBar(np, false);
            myCart = new Cart() { Title = "Cart" };
            np.BackgroundColor = Color.FromHex("#FFFFFF");
            this.Children.Add(pr);
            this.Children.Add(np);
            this.Children.Add(myCart);
        }
        
    }
}
