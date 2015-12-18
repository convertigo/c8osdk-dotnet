using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace retail_store
{
    public class Prod : Rayon
    {
        String sku;
        String priceOfUnit;

        public Prod(String name, String imageUrl, String id, String shopcode, String fatherId, String sku, String priceOfUnit) : base(name, imageUrl, id, shopcode, fatherId)
        {
            this.Sku = sku;
            this.PriceOfUnit = priceOfUnit;
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
                priceOfUnit = value +" €";
            }
        }

        
    }
}
