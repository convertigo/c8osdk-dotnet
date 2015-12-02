using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK.Listeners;
using Windows.UI.Popups;
using Convertigo.SDK.Exceptions;

using System.Windows.Controls;
using System.Windows;

namespace Sample01WindowsPhone
{
    class C8oExceptionListenerImpDefault : C8oExceptionListener
    {

        public C8oExceptionListenerImpDefault() : base((exception) => {
            String errorMessage = exception.Message;

            //if (exception is C8oCallException)
            //{
            //    C8oCallException c8oCallException = (C8oCallException) exception;
            //    if (c8oCallException.getWebResponse() != null) 
            //    {
            //        errorMessage += "\nStatus Code : " + c8oCallException.getWebResponse().Headers;
            //    }
            //}

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                System.Windows.MessageBox.Show(errorMessage);
            });
        })
        {

        }
    }
}
