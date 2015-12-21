using Sample05Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Sample05Wpf.Win
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Sample05 common;

        public MainWindow()
        {
            InitializeComponent();
            
            Debug.WriteLine("test");

            Sample05.cert = File.ReadAllBytes("D:\\COMMON\\C8O\\7.3.3_srv_win\\tomcat\\conf\\client.p12");

            common = new Sample05(
                output =>
                {
                    Output.Text = output;
                },
                debug =>
                {
                    Debug.WriteLine(debug);
                }
            );
        }

        private async void OnTest01(object sender, EventArgs args)
        {
            await common.OnTest01(sender, args);
        }

        private async void OnTest02(object sender, EventArgs args)
        {
            await common.OnTest02(sender, args);
        }

        private void OnTest03(object sender, EventArgs args)
        {
            common.OnTest03(sender, args);
        }

        private void OnTest04(object sender, EventArgs args)
        {
            common.OnTest04(sender, args);
        }

        private void OnTest05(object sender, EventArgs args)
        {
            common.OnTest05(sender, args);
        }

        private void OnTest06(object sender, EventArgs args)
        {
            common.OnTest06(sender, args);
        }

        private void OnTest07(object sender, EventArgs args)
        {
            common.OnTest07(sender, args);
        }

        private void OnTest08(object sender, EventArgs args)
        {
            common.OnTest08(sender, args);
        }

        private void OnTest09(object sender, EventArgs args)
        {
            common.OnTest09(sender, args);
        }

        private void OnTest10(object sender, EventArgs args)
        {
            common.OnTest10(sender, args);
        }

        private void OnTest11(object sender, EventArgs args)
        {
            common.OnTest11(sender, args);
        }

        private void OnTest12(object sender, EventArgs args)
        {
            common.OnTest12(sender, args);
        }

        private void OnTest13(object sender, EventArgs args)
        {
            common.OnTest13(sender, args);
        }

        private void OnTest14(object sender, EventArgs args)
        {
            common.OnTest14(sender, args);
        }
    }
}
