using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK.Listeners;

namespace Sample01XamarinForms
{
    class C8oExceptionListenerImpDefault : C8oExceptionListener
    {
        public C8oExceptionListenerImpDefault() : base ((exception) => {
            App.Current.MainPage.DisplayAlert("Error", exception.Message, "OK");
        })
        {

        }
    }
}
