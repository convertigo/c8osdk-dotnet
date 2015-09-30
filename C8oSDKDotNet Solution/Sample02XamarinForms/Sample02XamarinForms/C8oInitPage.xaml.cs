using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Sample02XamarinForms
{
    public partial class C8oInitPage : ContentPage
    {

        private FileReader fileReader;

        public C8oInitPage(FileReader fileReader)
        {
            this.fileReader = fileReader;
            InitializeComponent();
        }

        void OnValidateButtonClicked(object sender, EventArgs args)
        {
            String endpoint = this.endpointEntry.Text;
            this.endpointEntry.AddToHistory(endpoint);
            Navigation.PushAsync(new C8oCallPage(endpoint, this.fileReader));
        }
    }
}
