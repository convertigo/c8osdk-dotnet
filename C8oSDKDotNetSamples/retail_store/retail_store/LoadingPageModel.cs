using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace retail_store
{
    public class LoadingPageModel: INotifyPropertyChanged
    {
        public LoadingPageModel()
        {
            Current = "0";
            Total = "0";
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private string current;
        private string total;

        public string Current
        {
            get
            {
                return current;
            }

            set
            {
                current = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                } 
            }
        }

        public string Total
        {
            get
            {
                return total;
            }

            set
            {
                total = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }
    }
}
