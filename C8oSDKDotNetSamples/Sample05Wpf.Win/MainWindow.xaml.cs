using Convertigo.SDK;
using Sample05Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            
            common = new Sample05(
                code =>
                {
                    Dispatcher.BeginInvoke(code);
                },
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
    }
}
