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
        
        public event PropertyChangedEventHandler PropertyChanged;
        private string current;
        private string total;
        private string state;

        public LoadingPageModel()
        {
            Current = "0";
            Total = "0";
            State = "0 / 0";
        }

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

        public string State
        {
            get
            {
                return state;
            }

            set
            {
                state = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }
    }
}
