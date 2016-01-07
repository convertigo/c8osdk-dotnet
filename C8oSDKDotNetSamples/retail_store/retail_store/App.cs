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
        // We define the endpoint Url throw a constant variable
        const string conn = "http://192.168.100.86:18080/convertigo/projects/sampleMobileRetailStore";
        public static C8o myC8o;
        public static C8o myC8oCart;
        public static Dictionary<String, Object> models;
        public static CartViewModel cvm;
        public Boolean connectivity;
        public bool execution;
        public static LoadingPageModel LoadP;
        
        public App()
        {
            execution = false;
            connectivity = CrossConnectivity.Current.IsConnected;

            CrossConnectivity.Current.ConnectivityChanged += (sender, args) =>
            {
                connectivity = CrossConnectivity.Current.IsConnected;
                if (connectivity == true)
                {
                    CheckCartAfterConn();
                    OnStart();
                    ((TabbedPageP)this.MainPage).myCart.SetVisibility(true);
                }
                else
                {
                    ((TabbedPageP)this.MainPage).myCart.SetVisibility(false);
                }
            };

            //Here we are going to work with two C8o Objects. One for the catalogue and another for the cart.

            //instanciate C8o Object with attributes
            myC8o = new C8o(conn,                               // This variable is related to the end point URL.
                    new C8oSettings().                          //
                    SetTimeout(10000).                          // Here we set timeout to 10000 ms
                    SetTrustAllCertificates(true).              //
                    SetDefaultDatabaseName("retaildb").         // Here we define the default database name as "retaildb"
                    SetIsLogRemote(true)                        //
                );

            //instanciate C8o Object for our cart with attributes
            myC8oCart = new C8o(conn,                           // This variable is related to the end point URL.
                    new C8oSettings().                          //
                    SetTimeout(10000).                          // Here we set timeout to 10000 ms
                    SetTrustAllCertificates(true).              //
                    SetDefaultDatabaseName("cartdb").           // Here we define the default database name as "cartdb"
                    SetIsLogRemote(true)                        //
                );

            //instanciate respectively new Dictionary, CartViewModel , and LoadingPageModel.
            models = new Dictionary<string, object>();
            cvm = new CartViewModel();
            LoadP = new LoadingPageModel();

            /* Set the MainPage
            Here is a tabbedPage from wich we will be able to navigate into the whole application.*/
            MainPage = new LoadingPage();

            // If the device's operating system is IOS then we set a diffrent padding due to the header's floating bar
            if (Device.OS == TargetPlatform.iOS)
            {
                MainPage.Padding = new Thickness(0, 20, 0, 0);
            }
                
        }

    protected override async void OnStart()
        {

            // Handle when your app starts
            
            //if network state is ok then we can authentificate
            if (connectivity)
            {
                //instanciate a new JObject data that will recieve json from our C8o objects
                JObject data;
                data = await myC8o.CallJson(
                ".select_shop",          //We call here the "select_shop" sequence from the default project as the project has been define in the endpoint URL. 
                "shopCode", "42")        //And the sequence's variables 
                .Fail((e, p) =>
                {
                    Debug.WriteLine("LAA" + e);     //Handle errors..
                })
                .Async();                           //Async Call



                //CallJson Method is called thanks to C8o Object 
                // if data return "42" for selectshop then..
                if (data["document"]["shopCode"].ToString() == "42")
                {
                    //CallJson Method is called thanks to C8o Object 
                    await myC8o.CallJson(
                        "fs://.sync")           //We synchronize here the Catalogue from the default project on the mobile (fs://)
                                                //as the project has been define in the endpoint URL. 
                        .Progress(progress =>
                        {
                            LoadP.State = "" + progress.Current + "/" + progress.Total; // We recover the progression's state with Current and total attribute
                        })
                        .Fail((e, p) =>
                        {
                            Debug.WriteLine("" + e);     //Handle errors..
                        })
                        .Async();                       //Async Call
                }

                //CallJson Method is called thanks to C8o Object    
                await myC8oCart.CallJson(
                ".Connect",               //We call here the "Connect" sequence from the default project as the project has been define in the endpoint URL.
                "User", "User1")         //We give it parameters as the name of the FULLSYNC connector that we calls
                .Fail((e, p) =>
                {
                    Debug.WriteLine("" + e);     //Handle errors...
                })
                .Async();                           //Async Call

                //CallJson Method is called thanks to C8o Object  
                JObject Jobj;
                Jobj =  await myC8oCart.CallJson(
                    "fs://.sync",                       //We synchronize here the Cart from the default project as the project has been define in the endpoint URL. 
                    "continuous", true)                 //And set synchronization to true
                    .Progress(progress =>
                    {
                        if (progress.Finished == true)      // If initial replication is finished
                        {
                            if (progress.Pull)              //If replication's direction is pull then...
                            { 
                                App.cvm.GetRealPrice();     
                            }
                        }
                    })
                    .Fail((e, p) =>                         
                    {
                        Debug.WriteLine("" + e);           //Handle errors.. 
                    })
                    .Async();                             //Async Call


                CheckCartAfterConn();
            }
            if(!execution)
            {
                
                MainPage = new TabbedPageP();
                if (Device.OS == TargetPlatform.iOS)                // If target device's os is ios then...
                {
                    MainPage.Padding = new Thickness(0, 20, 0, 0);
                }
                execution = true;
               
            }
        }

        public async void CheckCartAfterConn()
        {
            // When we retrive Network we execute CartUpdated sequence on server
            await myC8oCart.CallJson(
                ".CartUpdated"          //We call here the "CartUpdated" sequence from the default project as the project has been define in the endpoint URL.
                )        
                .Fail((e, p) =>
                {
                    Debug.WriteLine("" + e);     //Handle errors..
                })
                .Async();                         //Async Call

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
