using Convertigo.SDK;
using System.Diagnostics;
using Xamarin.Forms;

namespace SampleBigFileTransfer
{
    public class App : Application
	{
        internal C8o c8o;
        internal C8oFileTransfer fileTransfer;

        public App()
        {
            C8oPlatform.Init();
            
            // The root page of your application
            MainPage = new Login(this);
            
            c8o = new C8o(
            //"http://tonus.twinsoft.fr:18080/convertigo/projects/BigFileTransferSample"
            //"http://nicolasa.convertigo.net/cems/projects/BigFileTransferSample"
            "http://192.168.100.69:18080/convertigo/projects/BigFileTransferSample"
            );

            fileTransfer = new C8oFileTransfer(c8o);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
