using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using App1.Models;
using App1.Views;
using App1.ViewModels;
using Newtonsoft.Json.Linq;

namespace App1.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ItemsPage : ContentPage
    {
        ItemsViewModel viewModel;

        public ItemsPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new ItemsViewModel();
        }

        async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var item = args.SelectedItem as Item;
            if (item == null)
                return;

            await Navigation.PushAsync(new ItemDetailPage(new ItemDetailViewModel(item)));

            // Manually deselect item.
            ItemsListView.SelectedItem = null;
        }

        async void AddItem_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("================> Start ===================");
            JObject data;    
            /*
            data = await ((App)Application.Current).c8o.CallJson(".LoginTesting").Async();
            System.Diagnostics.Debug.WriteLine("================> Login Done  ===================" + data.ToString());
            */

            data = await ((App)Application.Current).c8o.CallJson("fs://flightshare_fullsync.reset").Async();
            System.Diagnostics.Debug.WriteLine("================> Reset Done ==================="  +data.ToString());


            data = await ((App)Application.Current).c8o.CallJson("fs://flightshare_fullsync.sync")
            .Progress((status) =>  {
                // System.Diagnostics.Debug.WriteLine("================> Status " + status.ToString());
            })    
            .Async();

            System.Diagnostics.Debug.WriteLine("================> Sync Done ===================" + data.ToString());

            data = await ((App)Application.Current).c8o.CallJson("fs://flightshare_fullsync.view",
                "ddoc", "Design_document",
                "view", "AirportsByIACO"
            ).Async();
            System.Diagnostics.Debug.WriteLine("================> Data ===================" + data.ToString());

            await Navigation.PushModalAsync(new NavigationPage(new NewItemPage()));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (viewModel.Items.Count == 0)
                viewModel.LoadItemsCommand.Execute(null);
        }
    }
}