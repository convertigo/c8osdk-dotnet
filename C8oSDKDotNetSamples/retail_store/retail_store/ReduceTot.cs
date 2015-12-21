using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace retail_store
{
    public class ReduceTot: INotifyPropertyChanged
    {
        private string total;
        private string count;
        private string newPrice;
        private string discount;

        public event PropertyChangedEventHandler PropertyChanged;

        public ReduceTot(string total, string count, string newPrice, string discount)
        {
            this.Total = total;
            this.Count = count;
            this.NewPrice = newPrice;
            this.Discount = discount;
            
        }
        public ReduceTot(string total, string count)
        {
            this.Total = total;
            this.Count = count;
        }

        public ReduceTot()
        {

        }

        public string Total
        {
            get
            {
                return total;
            }

            set
            {
                total = "Estimated Price: "+ value + " €";
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
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
                count = "Total: "+value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        public string NewPrice
        {
            get
            {
                return newPrice + " €";
            }

            set
            {
                newPrice = "Real price: "+value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
                
                
            }
        }

        public string Discount
        {
            get
            {
                return discount;
            }

            set
            {
                discount = value +" of discount because of your fidelity !";
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }
    }
}
