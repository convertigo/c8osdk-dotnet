<p align="center">
  <img src="https://www.convertigo.com/wp-content/themes/EightDegree/images/logo_convertigo.png">
  <h2 align="center"> C8oSDK .Net</h2>
</p>
<p align="center">
  <a href="/LICENSE"><img src="https://img.shields.io/badge/License-Apache%202.0-blue.svg" alt="License"></a>
</p> 


## TOC

- [TOC](#toc)
- [Introduction](#introduction)

## Introduction ##

### About SDKs ###

This is the Convertigo library for .NET

Convertigo Client SDK is a set of libraries used by mobile or Windows desktop applications to access Convertigo Server services. An application using the SDK can easily access Convertigo services such as Sequences and Transactions.

The Client SDK will abstract the programmer from handling the communication protocols, local cache, FullSync off line data management, UI thread management and remote logging. So the developer can focus on building the application.

Client SDK is available for:
* [Android Native](https://github.com/convertigo/c8osdk-android) apps as a standard Gradle dependency
* [iOS native](https://github.com/convertigo/c8osdk-ios) apps as a standard Cocoapod
* [React Native](https://github.com/convertigo/react-native-c8osdk) as a NPM package
* [Google Angular framework](https://github.com/convertigo/c8osdk-angular) as typescript an NPM package
* [Vue.js](https://github.com/convertigo/c8osdk-js), [ReactJS](https://github.com/convertigo/c8osdk-js), [AngularJS](https://github.com/convertigo/c8osdk-js) Framework, or any [Javascript](https://github.com/convertigo/c8osdk-js) project as a standard Javascript NPM package
* [Windows desktop](https://github.com/convertigo/c8osdk-dotnet) or [Xamarin apps](https://github.com/convertigo/c8osdk-dotnet) as Nugets or Xamarin Components


This current package is the Native .NET SDK. For others SDKs see official [Convertigo Documentation.](https://www.convertigo.com/document/all/cmp-7/7-5-1/reference-manual/convertigo-mbaas-server/convertigo-client-sdk/programming-guide/)

### About Convertigo Platform ###

Convertigo Mobility Platform supports native .NET developers. Services brought by the platform are available for .NET clients applications thanks to the Convertigo MBaaS SDK. SDK provides an .NET framework you can use to access Convertigo Server’s services such as:

- Connectors to back-end data (SQL, NoSQL, REST/SOAP, SAP, - WEB HTML, AS/400, Mainframes)
- Server Side Business Logic (Protocol transform, Business logic augmentation, ...)
- Automatic offline replicated databases with FullSync technology
- Security and access control (Identity managers, LDAP , SAML, oAuth)
- Server side Cache
- Push notifications (APND, GCM)
- Auditing Analytics and logs (SQL, and Google Analytics)

[Convertigo Technology Overview](http://download.convertigo.com/webrepository/Marketing/ConvertigoTechnologyOverview.pdf)

[Access Convertigo mBaaS technical documentation](http://www.convertigo.com/document/latest/)

[Access Convertigo SDK Documentations](https://www.convertigo.com/document/all/cmp-7/7-5-1/reference-manual/convertigo-mbaas-server/convertigo-client-sdk/)

## Requirements ##

* Visual studio
* Windows or Mac OS

## Installation ##

Please use Nugets

## Documentation ##

### Initializing and creating a C8o instance for an Endpoint ###

For the .NET SDK, there is a common static initialization to be done before using the SDK feature. It prepares some platform specific features. After that, you will be able to create and use the C8o instance to interact with the Convertigo server and the Client SDK features. A C8o instance is linked to a server through is endpoint and cannot be changed after.

You can have as many C8o instances (except Angular), pointing to a same or different endpoint. Each instance handles its own session and settings. We strongly recommend using a single C8o instance per application because server licensing can based on the number of sessions used.

```csharp
// The .NET initialization for platform specific initialization, should be called once
C8oPlatform.Init();
…
var c8o = new C8o("https://demo.convertigo.net/cems/projects/sampleMobileCtfGallery");
// the C8o instance is ready to interact over https with the demo.convertigo.net server, using sampleMobileUsDirectoryDemo as default project.
```

### Advanced instance settings ###

The endpoint is the mandatory setting to get a C8o instance, but there is additional settings through the C8oSettings class.

A C8oSettings instance should be passed after the endpoint. Settings are copied inside the C8o instance and a C8oSettings instance can be modified and reused after the C8o constructor.

Setters of C8oSettings always return its own instance and can be chained.

A C8oSettings can be instantiated from an existing C8oSettings or C8o instance.

```csharp
// the common way
C8o c8o = new C8o(getApplicationContext(), "https://demo.convertigo.net/cems/projects/sampleMobileCtfGallery", new C8oSettings()
 .setDefaultDatabaseName("mydb_fullsync")
 .setTimeout(30000));

// the verbose way
String endpoing = "https://demo.convertigo.net/cems/projects/sampleMobileCtfGallery";
C8oSettings c8oSettings = new C8oSettings();
c8oSettings.setDefaultDatabaseName("mydb_fullsync");
c8oSettings.setTimeout(30000);
c8o = new C8o(getApplicationContext(), endpoint, c8oSettings);

// customize existing settings
C8oSettings customSettings = new C8oSettings(c8oSettings).setTimeout(60000);
// or from a C8o instance
customSettings = new C8oSettings(c8o).setTimeout(60000);

// all settings can be retrieve from a C8o or C8oSettings instance
int timeout = c8o.getTimeout();
```

### Calling a Convertigo Requestable ###

With a C8o instance you can call Convertigo Sequence and Transaction or make query to your local FullSync database. You must specify the result type you want: an XML Document or a JSON Object response.
  
#### Returning JSON ####
Just use the `c8o.callJson` method to request a JSON response.

```csharp
using System.Xml.Linq;
…
// c8o is a C8o instance
XDocument document = c8o.CallXml(".getSimpleData").Sync();
```

#### Returning XML ####
Just use the c8o.callXml method to request a XML response.

```csharp
using Newtonsoft.Json.Linq;
…
// c8o is a C8o instance
JObject jObject = c8o.CallJson(".getSimpleData").Sync();
```

### Call parameters ###

The call method expects the requester string of the following syntax:

- For a transaction: [project].connector.transaction  
- For a sequence: [project].sequence


The project name is optional, i.e. if not specified, the project specified in the endpoint will be used.  
Convertigo requestables generally need key/value parameters. The key is always a string and the value can be any object but a string is the standard case.  
Here a sample with JSON but this would be the same for XML calls:

#### The common way with parameters ####

```csharp
JObject jObject = c8o.CallJson(".getSimpleData",
  "firstname", "John",
  "lastname", "Doe"
).Sync();
```

	
#### The verbose way ####

```csharp
IDictionnary<string, object> parameters = new Dictionnary<string, object>();
parameters["firstname"] = "John";
parameters["lastname"] = "Doe";
JSONObject jObject = c8o.CallJson(".getSimpleData", parameters).Sync();
```

### Working with threads ###

#### Locking the current thread ####

Maybe you noticed that the calls methods doesn’t return the result directly and that all the sample code chains to the `.sync()` method.  
This is because the call methods return a `C8oPromise` instance. That allows the developer to choose if he wants to block the current thread, make an async request or get the response in a callback.  
The `.sync()` method locks the current thread and return the result as soon as it’s avalaible. Of course this should not be used in a UI thread as this will result to a frozen UI untill data is returned by the server. You should use the `.sync()` method only in worker threads.  

```csharp
// lock the current thread while the request is done
JObject jObject = c8o.CallJson(".getSimpleData").Sync();
// the response can be used in this scope
public async Task OnClick(object sender, EventArgs args)
{
  // doesn't lock the current thread, needs to be in a 'async' scope (function or method)
  JObject jObject = await c8o.CallJson(".getSimpleData").Async();
  // the response can be used in this scope
  …
}
```
    
#### Freeing the current thread ####

As in many cases, locking the current thread is not recommended, the `.then()` method allows to register a callback that will be executed on a worker thread.  
The `.thenUI()` method does the same but the callback will be executed on a UI thread. This is useful for quick UI widgets updates.  
The `.then()` and `.thenUI()` callbacks receives as parameters the response and the request parameters.

#### Then ####

```csharp
// doesn't lock the current thread while the request is done
c8o.CallJson(".getSimpleData").Then((jObject, parameters) =>
{
  // the jObject is available, the current code is executed in an another working thread
  …
  return null; // return null for a simple call
  }
});
// following lines are executed immediately, before the end of the request.
```
	
#### ThenUI ####

```csharp
c8o.CallJson(".getSimpleData").ThenUI((jObject, parameters) =>
{
  // the jObject is available, the current code is executed in the UI thread
  Output.Text = jObject.ToString();
  …
  return null; // return null for a simple call
  }
});
// following lines are executed immediately, before the end of the request.
```