using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK.Listeners;
using Xamarin.Forms;
using Convertigo.SDK.Exceptions;

namespace Sample01XamarinForms
{
    class C8oExceptionListenerImpCustom : C8oExceptionListener
    {
        private Label label;

        public C8oExceptionListenerImpCustom(Label label)
            : base((exception) =>
            {
                String errorMessage = exception.Message;

                //if (exception is C8oCallException)
                //{
                //    C8oCallException c8oCallException = (C8oCallException)exception;
                //    if (c8oCallException.getHttpResponseMessage() != null)
                //    {
                //        errorMessage += "\nStatus code : " + c8oCallException.getHttpResponseMessage().StatusCode;
                //    }
                //}

                label.Text = errorMessage;
            })
        {
            this.label = label;
        }
    }
}
