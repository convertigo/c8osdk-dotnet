using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;
using Convertigo.SDK;
using Convertigo.SDK.FullSync;
using Convertigo.SDK.Listeners;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Plugin.Connectivity;


namespace retail_store
{
    public class App : Application
    {   
        //instanciate static objects accessible from the whole solution. 
        public static C8o myC8o;
        public static C8o myC8oCart;
        public static Dictionary<String, Object> models;
        public static CartViewModel cvm;
        public Boolean connectivity;
        public bool exec;
        


        public App()
        {
            connectivity = CrossConnectivity.Current.IsConnected;
            CrossConnectivity.Current.ConnectivityChanged += (sender, args) =>
            {
                Debug.WriteLine("Connectivity Changed", "IsConnected: " + args.IsConnected.ToString(), "OK");
                connectivity = args.IsConnected;
                if (connectivity)
                {
                    OnStart();
                }
                
            };
            

            //instanciate C8o Objects with attributes
            myC8o = new C8o("http://192.168.100.86:18080/convertigo/projects/sampleMobileRetailStore",
                    new C8oSettings().
                    SetTimeout(10000).
                    SetTrustAllCertificates(true).
                    SetDefaultFullSyncDatabaseName("retaildb").
                    SetIsLogRemote(true)
                    
                   
                );

            //instanciate C8o Objects with attributes
            myC8oCart = new C8o("http://192.168.100.86:18080/convertigo/projects/sampleMobileRetailStore",
                    new C8oSettings().
                    SetTimeout(10000).
                    SetTrustAllCertificates(true).
                    SetDefaultFullSyncDatabaseName("cartdb").
                    SetIsLogRemote(true)


                );


            models = new Dictionary<string, object>();
            cvm = new CartViewModel();
            
            // Set the MainPage
            //It is a tabbedPage from wich we will be able to navigate into the whole application. 
            MainPage = new TabbedPageP();
            
        }

        

    protected override async void OnStart()
        {
            exec = true;
            // Handle when your app starts
            if (connectivity)
            {

                JObject jObj;
                jObj = await myC8o.CallJsonAsync(".select_shop",
                    new Dictionary<string, object> {
                    { "shopCode", "42" },
                    }
                );


                await MainPage.Navigation.PushModalAsync(new LoadingPage());
                myC8o.Call("fs://.sync", null,
                    new C8oJsonResponseListener((jsonResponse, parameters) =>
                    {
                        if(jsonResponse["status"].ToString() == "Stopped")
                        {
                            if (exec == true)
                            {
                                Cart();
                            }
                        }

                    }),
                    new C8oExceptionListener((exception, parameters) =>
                    {
                        Debug.WriteLine("Exeption : [ToString] = " + exception.ToString() + "Fin du [ToString]");
                    })
                );

                
                
                
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

        private async void Cart()
        {
            exec = false;
            JObject jObjCart;
            jObjCart = await myC8oCart.CallJsonAsync(".Connect");
            //Downloading designdoc from cartdb for fs://cartdb

            myC8oCart.Call("fs://.sync",
                new Dictionary<string, object> {
                    { "live", "true" },
                },
                new C8oJsonResponseListener((jsonResponse, parameters) =>
                {
                    //Debug.WriteLine(jsonResponse.ToString());
                    App.cvm.GetRealPrice();
                    App.cvm.GetReducePrice();

                }),
                new C8oExceptionListener((exception, parameters) =>
                {
                    Debug.WriteLine("Exeption : [ToString] = " + exception.ToString() + "Fin du [ToString]");
                })
            );
            await MainPage.Navigation.PopModalAsync();

          
        }

        
    }
}
