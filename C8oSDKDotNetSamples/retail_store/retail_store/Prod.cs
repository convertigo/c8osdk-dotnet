using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace retail_store
{
    public class Prod : Rayon
    {
        String sku;
        String priceOfUnit;
        private ImageSource imgs;
        //Mapping class
        public Prod(String name, String imageUrl, String id, String shopcode, String fatherId, String sku, String priceOfUnit, ImageSource imgs = null) : base(name, imageUrl, id, shopcode, fatherId)
        {
            this.Sku = sku;
            this.PriceOfUnit = priceOfUnit;
            this.imgs = imgs;
        }

        public string Sku
        {
            get
            {
                return sku;
            }

            set
            {
                sku = value;
            }
        }

        public string PriceOfUnit
        {
            get
            {
                return priceOfUnit;
            }

            set
            {
                if (value == null)
                {
                    value = "0";
                }
                priceOfUnit = value;
            }
        }

        public ImageSource Imgs
        {
            get
            {
                return imgs;
            }

            set
            {
                imgs = value;
            }
        }


    }
}
