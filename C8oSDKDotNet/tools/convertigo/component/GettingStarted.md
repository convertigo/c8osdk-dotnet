
# Getting Started with Convertigo MBaaS SDK
**Convertigo MBaaS SDK** makes it possible to connect Xamarin Apps to **Convertigo** Back end Services. To get started, first, follow these steps to create a backend service your app will call :

1. Register on Convertigo.com to get your free Convertigo Developper account. This will give you a free trial cloud access, a **PSC** (Personal Server Certificate) to start your Convertigo Studio and a free account on Convertigo support forums: [http://register.convertigo.com](http://register.convertigo.com "Register")


2. Download Convertigo Studio. This will enable you to create back end services called "sequences". You will then be able to call these sequences from your Xamarin apps: [http://sourceforge.net/projects/convertigo/files/latest/download ](http://sourceforge.net/projects/convertigo/files/latest/download "Download Convertigo Studio")

3. Download the retailStore backend sample project from here : [http://download.convertigo.com/retailSore.car](http://download.convertigo.com/retailSore.car "Download project")

4. Import sample project in Convertigo Studio : **File->Import->Convertigo->Convertigo Project**, then cick **next** and browse for the downloaded **.CAR** project, click **Next**  then **Finish**"

5. Deploy your sample retailStore backend project on the trial cloud: In Convertigo Studio, right click your imported project, then choose **Deploy** , **Skip** the version number and choose **trial.convertigo.net** the server list. Ignore the pre-filled account and password and hit **Deploy**. The project will be deployed on the trial Convertigo Cloud.

You will be ready now to start Xamarin client side programming :

## Initialize the SDK ##
Before using any SDK function you need to initialize the SDK. Although all other code calling the SDK can be used in a shared project the initialisation code must be done in an Android or iOS project. The best place to initialize the SDK in in the:

- For Android in the MainActivity.cs:

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			global::Xamarin.Forms.Forms.Init (this, bundle);
			// Initialize the SDK Here.
			C8oPlatform.Init();

			LoadApplication (new App ());
		}

- for iOS in the AppDelegate.cs

		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init ();
			LoadApplication (new AppTestSDK.App ());
			// Initialize the SDK Here.
			C8oPlatform.Init();

			return base.FinishedLaunching (app, options);
		}



## Create a Convertigo End Point: ##
In Xamarin Studio or Visual Studio 2015, Use this to create a **C8o** end point Object (C8o stands for "Convertigo") in your app:

	using Convertigo.SDK;
	using Newtonsoft.Json.Linq;
    
    C8o myC8o = new C8o("http://trial.convertigo.net/cems/projects/retailStore");

This will create an End point on on trial Convertigo Cloud (**http://trial.convertigo.net/cems/projects**) and select default project to be the **retailStore** project we deployed previously. 

## Use it:##
To call a service in the server just use the **CallJSON** method  passing the reference to the service you want to call. A service reference, also called a "**requestable**" is in this form :
> **project.service**

As the project has been specified in the end point, we just have to call the service of the default project. In this case the **select_shop** service. You can pass any number of key/value pairs to the call . They will be automatically mapped to sequence variables on the server. In this case, the **selectShop** sequence takes a **shopCode** variable we want to set to **42**. 


    JObject data = await myC8o.CallJson (".select_shop", "shopCode", "42").Async ();


CallJSON is asynchronous and returns a **promise** object. You can use on it the **Async()** method to wait asynchronously for the server response without blocking the current thread with an **await** operator. Response is a **JObject** containing all the server response data. You can access the data using Linq:

    String returnedShopCode = (String)data ["document"]["shopCode"];

If you prefer to use the promise chaining mode you can use the promise object in this way :

	myC8o.CallJson (".select_shop",								// This is the requestable
		"shopCode", "42"										// The key/value parameters to the sequence
	).Then ((response, parameters) => {							// This will run as soon as the Convertigo server responds
		// do my stuff in a	 worker thread						// This is worker thread not suitable to update UI
		String sc = (String)response ["document"]["shopCode"];	// Get the data using Linq
		myC8o.Log (C8oLogLevel.DEBUG, sc);						// Log data on the Convertigo Server
		return null;											// last step of the promise chain, return null
	});

	
## Full Convertigo SDK Documentation ##
You can get all the detailed SDK documentations here:

[http://www.convertigo.com/document/next/7-4-0/reference-manual/convertigo-sdk/](http://www.convertigo.com/document/next/7-4-0/reference-manual/convertigo-sdk/ "SDK Documentation")

## Convertigo Back End services Quick Start Videos##
If you want more information on Convertigo back end services programming, have a a look to these quick start videos:

- [http://www.convertigo.com/rssatom-feeds/](http://www.convertigo.com/rssatom-feeds/)
- [http://www.convertigo.com/filter-out-data-to-the-mobile/](http://www.convertigo.com/filter-out-data-to-the-mobile/)
- [http://www.convertigo.com/mix-data-sources/](http://www.convertigo.com/mix-data-sources/)
- [http://www.convertigo.com/connect-to-sql-data-sources/](http://www.convertigo.com/connect-to-sql-data-sources/)
- [http://www.convertigo.com/compute-business-logic/](http://www.convertigo.com/compute-business-logic/)
- [http://www.convertigo.com/connect-to-soap-web-services/](http://www.convertigo.com/connect-to-soap-web-services/)


Enjoy Convertigo as much as we enjoyed building it!

(c) Convertigo 2016