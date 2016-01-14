using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace retail_store
{
    public class Rayon : INotifyPropertyChanged
    {
        String name;
        String imageUrl;
        String id;
        String shopcode;
        String fatherId;
        ImageSource img;
        public event PropertyChangedEventHandler PropertyChanged;
        //Mapping class
        public Rayon(String name, String imageUrl, String id, String shopcode, String fatherId)
        {

            this.name = name;
            this.imageUrl = imageUrl;
            this.id = id;
            this.shopcode = shopcode;
            this.fatherId = fatherId; 
        }

        //Getters and Setters.
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public string ImageUrl
        {
            get
            {
                return imageUrl;
            }

            set
            {
                imageUrl = value;
            }
        }

        public string Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }


        public string Shopcode
        {
            get
            {
                return shopcode;
            }

            set
            {
                shopcode = value;
            }
        }

        public string FatherId
        {
            get
            {
                return fatherId;
            }

            set
            {
                fatherId = value;
            }
        }

        public ImageSource Img
        {
            get
            {
                return img;
            }

            set
            {
                img = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }
    };
}
