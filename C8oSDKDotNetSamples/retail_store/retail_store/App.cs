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
        public static LoadingPageModel LoadP;
        

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
            LoadP = new LoadingPageModel();

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
                ".select_shop",          //We give him parameters as the name of the sequence that we calls
                "shopCode", "42")       //And the parameters for the sequence    
                .Fail((e, p) => 
                {
                    Debug.WriteLine("LAA"+e);//Handle errors..
                })
                .Async();               //Async Call

            // if data return "42" for selectshop then..
            if (data["document"]["shopCode"].ToString() == "42")
            {
                //Open the modal page in order to give the state of the waiting
                await MainPage.Navigation.PushModalAsync(new LoadingPage());

                //CallJson Method is called thanks to C8o Object 
                data = await myC8o.CallJson(
                    "fs://.sync")           //We give him parameters as the name of the FULLSYNC connector that we calls
                    .Progress(progress => 
                    {
                        LoadP.Current = "" + progress.Current;
                        LoadP.Total= "/ " + progress.Total;
                        Debug.WriteLine(""+progress.Current +"/"+ progress.Total); //We are able to obtain the progress of the task
                    })
                    .Fail((e, p) =>
                    {
                        Debug.WriteLine("LAA" + e);//Handle errors..
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
                    Debug.WriteLine("LAA" + e);//Handle errors...
                })
                .Async();

            //CallJson Method is called thanks to C8o Object 
            data = await myC8oCart.CallJson(
                    "fs://.sync")                   //We give him parameters as the name of the FULLSYNC connector that we calls
                     .Fail((e,p) =>                //And the live sync
                     {
                        Debug.WriteLine("" + e);
                    })
                    .Async();

            data = await myC8oCart.CallJson(
                    "fs://.sync",                       //We give him parameters as the name of the FULLSYNC connector that we calls
                    "continuous", true)               //And the live sync
                    .Progress(progress =>
                    {
                        App.cvm.GetRealPrice();
                        /*App.cvm.GetReducePrice();*/
                    })
                    .Fail((e, p) =>
                    {
                        Debug.WriteLine("" + e);
                    })
                    .Async();



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
