using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace retail_store
{
    public partial class LoadingPage : ContentPage
    {
        public LoadingPage()
        {
            InitializeComponent();
            current.BindingContext = App.LoadP;
            message.BindingContext = App.LoadP;
            message2.BindingContext = App.LoadP;
            message3.BindingContext = App.LoadP;

        }
    }
}
