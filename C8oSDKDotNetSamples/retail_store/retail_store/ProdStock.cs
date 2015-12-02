using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace retail_store
{
    public class ProdStock : Prod, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        float count;

        public ProdStock(String name, String imageUrl, String id, String shopcode, String fatherId, String sku, String priceOfUnit, float count):base( name,  imageUrl,  id,  shopcode,  fatherId,  sku,  priceOfUnit)
        {
            Count = count;
            
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
