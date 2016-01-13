using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace retail_store
{
    public partial class CategoryTablet : MasterDetailPage
    {
        public List<Page> ListPage;

        public CategoryTablet()
        {
            ListPage = new List<Page>();
            InitializeComponent();
            Title = "Category";
            // new NavigationPage(new Category()) { Title = "Products" };
            Master = new NavigationPage(new Category()) { Title = "return" };
            Detail = new contentDetailTablet();
           this.Detail.BackgroundColor = Color.Gray;
        }
        protected override bool OnBackButtonPressed()
        {
            if (ListPage.Count > 0)
            {
                Master = ListPage.Last();
                ListPage.Remove(ListPage.Last());
                
            }
            else
            {
                
                base.OnBackButtonPressed();
            }
            Detail = new contentDetailTablet();
            return true;
        }






        }

    }
