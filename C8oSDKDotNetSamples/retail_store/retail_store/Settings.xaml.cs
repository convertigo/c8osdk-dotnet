using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace retail_store
{
    public partial class Settings : ContentPage
    {
        public Settings()
        {
            InitializeComponent();
        }

        public async void Connexion(object sender, EventArgs e)
        {
            await DisplayAlert("Alert !", "Would you realy wants to delete these items ?", "Yes", "No");
        }
    }
}
