using NUnit.Framework;
using System.Windows;

namespace C8oSDKNUnitWPF
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    [TestFixture]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        public void Sub()
        {
            Assert.AreEqual(4, 6 - 2);
        }
        
    }
}
