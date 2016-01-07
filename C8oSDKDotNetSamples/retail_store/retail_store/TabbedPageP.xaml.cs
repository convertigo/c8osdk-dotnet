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
        public CategoryTablet tabletP;
        public Cart myCart;
        public TabbedPageP()
        {
            InitializeComponent();
            pr = new NavigationPage(new Products()) { Title = "Products"};
            this.Children.Add(pr);
            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
                tabletP = new CategoryTablet() { Title = "Category" };
                this.Children.Add(tabletP);
            }
            else
            {
                np = new NavigationPage(new Category()) { Title = "Category" };
                this.Children.Add(np);
                np.BackgroundColor = Color.FromHex("#FFFFFF");
            }
            myCart = new Cart() { Title = "Cart" };
            this.Children.Add(myCart);
        }
        
    }
}
