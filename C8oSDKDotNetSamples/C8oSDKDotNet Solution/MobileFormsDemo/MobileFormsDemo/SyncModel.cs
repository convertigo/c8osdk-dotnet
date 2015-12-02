using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using Xamarin.Forms;

using Newtonsoft.Json.Linq;


namespace MobileFormsDemo
{
    // This is the model for our progress. Holds a Progress property
    class SyncModel : INotifyPropertyChanged, Model
    {
        public event PropertyChangedEventHandler PropertyChanged;
        String progress;

        public SyncModel ()
        {
            // regsiter this model in the App opbject so it can be retreived from anywhere..
            App.models.Add("SyncModel", this);
        }

        public String Progress
        {
            set
            {
                progress = value;
                PropertyChanged(this, new PropertyChangedEventArgs(null));
            }
            get
            {
                return progress;
            }
        }

        public void PopulateData(JObject json)
        {
            this.Progress = "Replicating:" + (String)json["current"]+"/"+ (String)json["total"];
        }
    }
}
