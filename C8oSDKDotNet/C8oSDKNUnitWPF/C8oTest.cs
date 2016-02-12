using NUnit.Framework;
using System;
using System.Threading;

namespace C8oSDKNUnitWPF
{

    [TestFixture]
    class C8oTest
    {
        [Test]
        public void Add()
        {
           Assert.AreEqual(4, 2+2);
        }

        [Test]
        public void Win()
        {
            CrossThreadTestRunner runner = new CrossThreadTestRunner();
            runner.RunInSTA(
              delegate
              {
                  Console.WriteLine(Thread.CurrentThread.GetApartmentState());

                  MainWindow window = new MainWindow();
                  window.Show();

                  window.Sub();
              });
            Console.WriteLine(Thread.CurrentThread.GetApartmentState());
        }
    }
}
