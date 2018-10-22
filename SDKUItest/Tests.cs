using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace SDKUItest
{
    [TestFixture(Platform.Android)]
    [TestFixture(Platform.iOS)]
    public class Tests
    {
        IApp app;
        Platform platform;

        public Tests(Platform platform)
        {
            this.platform = platform;
        }

        [SetUp]
        public void BeforeEachTest()
        {
            app = AppInitializer.StartApp(platform);
        }

        [Test]
        public void WelcomeTextIsDisplayed()
        {
            AppResult[] results;

            results = app.WaitForElement(c => c.Marked("Run Tests"));
            app.Screenshot("Welcome screen.");

            // app.Tap("Run Tests");
            /*
            results = app.WaitForElement(c => c.Marked("Passed"),
                "Timeout wating for 'Passed'",
                new TimeSpan(0, 4, 0)
            );*/

            Assert.IsTrue(results.Any());
        }
    }
}
