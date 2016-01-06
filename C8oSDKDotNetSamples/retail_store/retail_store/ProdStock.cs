using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace retail_store
{
    public class ProdStock : Prod, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        float count;
        string priceEur;

        public ProdStock(String name, String imageUrl, String id, String shopcode, String fatherId, String sku, String priceOfUnit, float count):base( name,  imageUrl,  id,  shopcode,  fatherId,  sku,  priceOfUnit)
        {
            Count = count;
            priceEur = priceOfUnit + " €";
        }

        public string PriceEur
        {
            get
            {
                return base.PriceOfUnit.ToString() + " €";
            }
        }
        public string PriceEurCount
        {
            get
            {
                return ((Convert.ToDouble(base.PriceOfUnit, CultureInfo.InvariantCulture)) * (this.Count)).ToString() + " €";
            }
        }
        public float Count
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
