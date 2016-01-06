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
        const string conn = "";
     
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
            exec = false;
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
            myC8o = new C8o("http://192.168.100.86:18080/convertigo/projects/sampleMobileRetailStore",      // This variable is related to the end point URL.
                    new C8oSettings().                                                                      //
                    SetTimeout(10000).                                                                      // Here we set timeout to 10000 ms
                    SetTrustAllCertificates(true).                                                          //
                    SetDefaultDatabaseName("retaildb").                                                     // Here we define the default database name as "retaildb"
                    SetIsLogRemote(true)                                                                    //
                );

            //instanciate C8o Object for our cart with attributes
            myC8oCart = new C8o("http://192.168.100.86:18080/convertigo/projects/sampleMobileRetailStore",  // This variable is related to the end point URL.
                    new C8oSettings().                                                                      //
                    SetTimeout(10000).                                                                      // Here we set timeout to 10000 ms
                    SetTrustAllCertificates(true).                                                          //
                    SetDefaultDatabaseName("cartdb").                                                       // Here we define the default database name as "cartdb"
                    SetIsLogRemote(true)                                                                    //
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
                        "fs://.sync")           //We synchronize here the Catalogue from the default project as the project has been define in the endpoint URL. 
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
                ".Connect",                 
                "User", "User1")         //We give it parameters as the name of the FULLSYNC connector that we calls
                .Fail((e, p) =>
                {
                    Debug.WriteLine("LAA" + e);//Handle errors...
                })
                .Async();

                //CallJson Method is called thanks to C8o Object  
                JObject Jobj;
                Jobj =  await myC8oCart.CallJson(
                    "fs://.sync",                       //We give it parameters as the name of the FULLSYNC connector that we calls
                    "continuous", true)               //And the live synchronization
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
                    .Async();   
                
                                           
                CheckCartAfterConn();
            }
            if(!exec)
            {
                MainPage = new TabbedPageP();
                if (Device.OS == TargetPlatform.iOS)
                {
                    MainPage.Padding = new Thickness(0, 20, 0, 0);
                }
                exec = true;
               
            }
        }

        public async void CheckCartAfterConn()
        {
            // When we retrive Network we execute CartUpdated sequence on server
            await myC8oCart.CallJson(
                ".CartUpdated"          //We give him parameters as the name of the sequence that we calls
                )       //And the parameters for the sequence    
                .Fail((e, p) =>
                {
                    Debug.WriteLine("LAA" + e);//Handle errors..
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
