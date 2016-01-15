using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace retail_store
{
    public partial class Detail : ContentPage, INotifyPropertyChanged
    {
        private Prod prod;
        private string count;
        public event PropertyChangedEventHandler PropertyChanged;
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
            //labelImage.BindingContext = DependencyService.Get<IDisplay>();
            labelImage.WidthRequest = DependencyService.Get<IDisplay>().Width;
            labelImage.HeightRequest = DependencyService.Get<IDisplay>().Height;
            if (Device.OS == TargetPlatform.iOS)
            {
                NavigationPage.SetHasNavigationBar(this, true);
            }
            else
            {
                NavigationPage.SetHasNavigationBar(this, false);
            }
            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
               
            }
            searchUnit();
            labelCount.BindingContext = this;

           // Debug.WriteLine("Height: "+ DependencyService.Get<IDisplay>().Height+" Width: "+ DependencyService.Get<IDisplay>().Width);
        }
        public void searchUnit()
        {   
            foreach (ProdStock item in App.cvm.ProductStock)
            {
                if (item.Id == prod.Id)
                {
                    this.Count = item.Count.ToString();
                    break;
                }
                else
                {
                    this.Count = "0";
                }
            }
            if (Convert.ToInt16(this.Count) < 0 || this.Count == null)
            {
                this.Count = "0";
            }
        }
        

        public void tapImage_Tapped(object sender, EventArgs e)
        {
            string imageName = ((FileImageSource)((Xamarin.Forms.Image)sender).Source).File.ToString();
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
            searchUnit();
        }

        protected override void OnAppearing()
        {
            searchUnit();
        }

        //Getters and Setters
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

        public string Count
        {
            get
            {
                return count;
            }

            set
            {
                count = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }


    }
}
