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
        ContentPage cp;
        public CategoryTablet()
        {
            ListPage = new List<Page>();
            InitializeComponent();
            Title = "Category";
            BackButtonPressedEventArgs bButon = new BackButtonPressedEventArgs();
            if (bButon.Handled)
            {
                int a = 1;
            }


            Master = new Category();

           
            cp= new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                {
                    new Label { Text = "Select a Category", FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)) }
                }
                }

            };
            Detail = cp;
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
            Detail = cp;
            return true;
        }






        }

    }
