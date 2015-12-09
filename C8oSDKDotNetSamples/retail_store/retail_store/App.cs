using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Convertigo.SDK;
using System.Diagnostics;
using Plugin.Connectivity;
using Newtonsoft.Json.Linq;


namespace retail_store
{
    public class App : Application
    {   
        //instanciate static objects accessible from the whole solution. 
        public static C8o myC8o;
        public static C8o myC8oCart;
        public static Dictionary<String, Object> models;
        public static CartViewModel cvm;
        //instanciate not static objects accessible from the whole solution. 
        public Boolean connectivity;
        public bool exec;
        

        public App()
        {
            //instanciate C8o Object with attributes
            myC8o = new C8o("http://192.168.100.86:18080/convertigo/projects/sampleMobileRetailStore",
                    new C8oSettings().
                    SetTimeout(10000).
                    SetTrustAllCertificates(true).
                    SetDefaultDatabaseName("retaildb").
                    SetIsLogRemote(true)
                );

            //instanciate C8o Object for our cart with attributes
            myC8oCart = new C8o("http://192.168.100.86:18080/convertigo/projects/sampleMobileRetailStore",
                    new C8oSettings().
                    SetTimeout(10000).
                    SetTrustAllCertificates(true).
                    SetDefaultDatabaseName("cartdb").
                    SetIsLogRemote(true)
                );

            //instanciate dictionnary
            models = new Dictionary<string, object>();
            cvm = new CartViewModel();
            
            /* Set the MainPage
            It is a tabbedPage from wich we will be able to navigate into the whole application.*/
            MainPage = new TabbedPageP();
        }

        

    protected override async void OnStart()
        {
            // Handle when your app starts

            //instanciate a new JObject data that will recieve json from our C8o objects
            JObject data;

            //CallJson Method is called thanks to C8o Object 
            data = await myC8o.CallJson(
                "select_shop",          //We give him parameters as the name of the sequence that we calls
                "shopCode", "42")       //And the parameters for the sequence    
                .Fail((e, p) => 
                {
                    //Handle errors..
                })
                .Async();               //Async Call

            //if data return "42" for selectshop then..
            if ((String) data["document"]["selectShop"] == "42")
            {
                //Open the modal page in order to give the state of the waiting
                await MainPage.Navigation.PushModalAsync(new LoadingPage());

                //CallJson Method is called thanks to C8o Object 
                data = await myC8o.CallJson(
                    "fs://.sync")           //We give him parameters as the name of the FULLSYNC connector that we calls
                    .Progress(progress => 
                    {
                        var complete = progress.Current / progress.Total * 100; //We are able to obtain the progress of the task
                    })
                    .Fail((e, p) =>
                    {
                        //Handle errors..
                    })
                    .Async();
                //Close the modal page that give us the progress...
               await MainPage.Navigation.PopModalAsync();
            }

            //CallJson Method is called thanks to C8o Object    
            JObject dataCart = await myC8oCart.CallJson(
                ".Connect")         //We give him parameters as the name of the FULLSYNC connector that we calls
                .Fail((e, p) =>
                {
                    //Handle errors...
                })
                .Async();

            //CallJson Method is called thanks to C8o Object 
            data = await myC8o.CallJson(
                    "fs://.sync",                   //We give him parameters as the name of the FULLSYNC connector that we calls
                    "live", "true")                 //And the live sync
                    .Progress(progress =>
                    {
                        //We are able to obtain the progress of the task
                        var complete = progress.Current / progress.Total * 100;

                        //And test for example if the task is done
                        if (progress.Finished)
                        {
                            App.cvm.GetRealPrice();
                            App.cvm.GetReducePrice();
                            MainPage.Navigation.PopModalAsync();
                        }
                            
                    })
                    .Async();


            }
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
