using Convertigo.SDK;
//using Convertigo.SDK.Listeners;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace retail_store
{
    public partial class Detail : ContentPage
    {
        private Prod prod;

        public Detail(Prod prod)
        {
            InitializeComponent();

            this.prod = prod;
            this.BindingContext = Prod;

            //Creating TapGestureRecognizers  
            var tapImage = new TapGestureRecognizer();
            //Binding events  
            tapImage.Tapped += tapImage_Tapped;
            //Associating tap events to the image buttons  
            //Image2.GestureRecognizers.Add(tapImage);
            Image3.GestureRecognizers.Add(tapImage);
            Image2.GestureRecognizers.Add(tapImage);
        }

        public Prod Prod
        {
            get
            {
                return prod;
            }

            set
            {
                prod = value;
            }
        }
        
        
    void tapImage_Tapped(object sender, EventArgs e)
    {

            
            string imageName = ((FileImageSource)((Image)sender).Source).File.ToString();
            switch (imageName)
            {
                case "plus.png":
                    App.cvm.Product = Prod;
                    App.cvm.CheckCart(true);
                    break;
                case "moins.png":
                    App.cvm.Product = Prod;
                    App.cvm.CheckCart(false);
                    break;
            }
            
        }


}
}
