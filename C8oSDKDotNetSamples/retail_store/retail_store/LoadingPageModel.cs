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
        private string task;
        private string message;
        private string message2;
        private string message3;

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

        public string Task
        {
            get
            {
                return task;
            }

            set
            {
                task = value;
                switch (task)
                {
                    case "check_connectivity":
                        Message = "We are checking for connectivity";
                        Message2 = "...";
                        Message3 = "";
                        break;
                    case "check_database":
                        Message = "We are checking database";
                        Message2 = "...";
                        Message3 = "";
                        break;
                    case "update_articles":
                        Message = "We are downloading articles for the first use"; 
                        Message3 = "This may take a few minutes";
                        Message2 = "There is about 3771 articles";
                        break;
                    case "update_cart":
                        Message = "We are synchronizing your cart.";
                        Message2 = "It's alomost finished";
                        Message3 = "This will take a few seconds";
                        break;
                }
            }
        }

        public string Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        public string Message2
        {
            get
            {
                return message2;
            }

            set
            {
                message2 = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        public string Message3
        {
            get
            {
                return message3;
            }

            set
            {
                message3 = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(null));
                }
            }
        }
    }
}
