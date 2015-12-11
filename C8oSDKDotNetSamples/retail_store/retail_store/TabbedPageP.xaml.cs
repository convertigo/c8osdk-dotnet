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
        public TabbedPageP()
        {
            InitializeComponent();
            NavigationPage pr = new NavigationPage(new Products()) { Title = "Products" };
            NavigationPage.SetHasNavigationBar(pr, false);
            NavigationPage np = new NavigationPage(new Category()) { Title = "Category" };
            NavigationPage.SetHasNavigationBar(np, false);
            Cart b = new Cart() { Title = "Cart" };
            np.BackgroundColor = Color.FromHex("#FFFFFF");
            this.Children.Add(pr);
            this.Children.Add(np);
            this.Children.Add(b);
        }
        
    }
}
