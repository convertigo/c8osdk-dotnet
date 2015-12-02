using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convertigo.SDK.Listeners;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;

namespace Sample01WindowsPhone
{
    class C8oExceptionListenerImpCustom : C8oExceptionListener
    {
        private TextBlock textBlock;

        public C8oExceptionListenerImpCustom(TextBlock textBlock)
            : base((exception) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    textBlock.Text = exception.ToString();
                });
            })
        {
            this.textBlock = textBlock;
        }
    }
}
