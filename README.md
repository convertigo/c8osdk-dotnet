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
  - [About SDKs](#about-sdks)
  - [About Convertigo Platform](#about-convertigo-platform)
- [Requirements](#requirements)
- [Installation](#installation)
- [Documentation](#documentation)
  - [Initializing and creating a C8o instance for an Endpoint](#initializing-and-creating-a-c8o-instance-for-an-endpoint)
  - [Advanced instance settings](#advanced-instance-settings)
  - [Calling a Convertigo Requestable](#calling-a-convertigo-requestable)
    - [Returning JSON](#returning-json)
    - [Returning XML](#returning-xml)
  - [Call parameters](#call-parameters)
    - [The common way with parameters](#the-common-way-with-parameters)
    - [The verbose way](#the-verbose-way)
  - [Working with threads](#working-with-threads)
    - [Locking the current thread](#locking-the-current-thread)
    - [Freeing the current thread](#freeing-the-current-thread)
    - [Then](#then)
    - [ThenUI](#thenui)
  - [Chaining calls](#chaining-calls)
  - [Handling failures](#handling-failures)
    - [Try / catch handling](#try--catch-handling)
    - [Then / ThenUI handling](#then--thenui-handling)
  - [Writing the device logs to the Convertigo server](#writing-the-device-logs-to-the-convertigo-server)
    - [Basic](#basic)
    - [Advanced](#advanced)
  - [Using the Local Cache](#using-the-local-cache)
  - [Using the Full Sync](#using-the-full-sync)
  - [Replicating Full Sync databases](#replicating-full-sync-databases)
  - [Replicating Full Sync databases with continuous flag](#replicating-full-sync-databases-with-continuous-flag)
  - [Full Sync FS_LIVE requests](#full-sync-fs_live-requests)
  - [Full Sync Change Listener](#full-sync-change-listener)

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

### Chaining calls ###

The `.then()` or `.thenUI()` returns a C8oPromise that can be use to chain other promise methods, such as `.then()` or `.thenUI()` or failure handlers.  
 The last `.then()` or `.thenUI()` must return a nil value. `.then()` or `.thenUI()` can be mixed but the returning type must be the same: XML or JSON.
 
```csharp
c8o.CallJson(".getSimpleData", "callNumber", 1).Then((jObject, parameters) =>
{
  // you can do stuff here and return the next C8oPromise<JObject> instead of deep nested blocks
  return c8o.CallJson(".getSimpleData", "callNumber", 2);
}).ThenUI((jObject, parameters) => // use .Then or .ThenUI is allowed
{
  // you can do stuff here and even modify previous parameters
  parameters["callNumber"] = 3;
  parameters["extraParameter"] = "ok";
  return c8o.CallJson(".getSimpleData", parameters);
}).Then((jObject, parameters) =>
{
  // you can do stuff here and return null because this is the end of the chain
  return null;
});
```

### Handling failures ###

A call can throw an error for many reasons: technical failure, network error and so on.  
The standard do/catch should be used to handle this.  
This is the case for the `.sync()` method: if an exception occurs during the request execution, the original exception is thrown by the method and can be encapsulated in a `C8oException`.

#### Try / catch handling ####

```csharp
try
{
  c8o.CallJson(".getSimpleData").Sync();
  // or in an async scope
  await c8o.CallJson(".getSimpleData").Async();
}
catch (Exception exception)
{
  // process the exception
}
```

#### Then / ThenUI handling ####

When you use the `.then()` or the `.thenUI()` methods, the do/catch mechanism can’t catch a “future” exception or throwable: you have to use the `.fail()` or `.failUI()` methods at the end on the promise chain.  
One fail handler per promise chain is allowed. The fail callback provide the object thrown (like an Exception) and the parameters of the failed request.

```csharp
c8o.CallJson(".getSimpleData", "callNumber", 1).Then((jObject, parameters) =>
{
  return c8o.CallJson(".getSimpleData", "callNumber", 2);
}).ThenUI((jObject, parameters) =>
{
  return null;
}).Fail((exception, parameters) =>
{
    // exception caught from the first or the second CallJson, can be an Exception
    // this code runs in a worker thread
    …
});

c8o.CallJson(".getSimpleData", "callNumber", 1).Then((jObject, parameters) =>
{
  return c8o.CallJson(".getSimpleData", "callNumber", 2);
}).ThenUI((jObject, parameters) =>
{
  return null;
}).FailUI((exception, parameters) =>
{
    // exception caught from the first or the second CallJson, can be an Exception
    // this code runs in a UI thread
    …
});
```

### Writing the device logs to the Convertigo server ###

#### Basic ####

An application developer usually adds log information in his code. This is useful for the code execution tracking, statistics or debugging.

The Convertigo Client SDK offers an API to easily log on the standard device logger, generally in a dedicated console. To see this console, a device must be physically connected on a computer.

Fortunately, the same API also send log to the Convertigo server and they are merged with the server log. You can easily debug your device and server code on the same screen, on the same timeline. Logs from a device contain metadata, such as the device UUID and can help to filter logs on the server.

A log level must be specified:

* Fatal: used for critical error message
* Error: used for common error message
* Warn: used for not expected case
* Info: used for high level messages
* Debug: used for help the developer to understand the execution
* Trace: used for help the developer to trace the code
* To write a log string, use the C8oLogger instance of a C8o instance:

```csharp
try
{
  c8o.Log.Info("hello world!"); // the message can be a simple string
}
catch (Exception exception)
{
  c8o.Log.Error("bye world...", e); // the message can also take an Exception argument
}
if (c8o.Log.IsDebug()) // check if currents log levels are enough
{
  // enter here only if a log level is 'trace' or 'debug', can prevent unnecessary CPU usage
  string msg = serializeData(); // compute a special string, like a Document serialization
  c8o.Log.Debug(msg);
}
```

#### Advanced ####

A C8oLogger have 2 log levels, one for local logging and the other for the remote logging. With the Android SDK, the local logging is set by the logcat options. With the .Net SDK, the local logging depends of the LogLevelLocal setting of C8oSettings.

The remote logging level is enslaved by Convertigo server Log levels property: devices output logger. In case of failure, the remote logging is disabled and cannot be re-enabled for the current C8o instance. It can also be disabled using the LogRemote setting of C8oSettings, enabled with true (default) and disabled with false.

To monitor remote logging failure, a LogOnFail handler can be registered with the C8oSetting.

The Convertigo Client SDK itself writes logs. They can be turned off using the LogC8o setting of C8oSettings, enabled with true (default) and disabled with false.

```csharp
new C8oSetting()
  .SetLogC8o(false)    // disable log from the Convertigo Client SDK itself
  .SetLogRemote(false) // disable remote logging
  .SetLogLevelLocal(C8oLogLevel.TRACE);
// or
new C8oSetting().SetLogOnFail((exception, parameters) =>
{
  // the exception contains the cause of the remote logging failure
});
```


### Using the Local Cache ###

Sometimes we would like to use local cache on C8o calls and responses, in order to:

* save network traffic between the device and the server,
* be able to display data when the device is not connected to the network.

The Local Cache feature allows to store locally on the device the responses to a C8o call, using the variables and their values as cache key.

To use the Local Cache, add to a call a pair parameter of "__localCache" and a C8oLocalCache instance. The constructor of C8oLocalCache needs some parameters:

* C8oLocalCache.Priority (SERVER / LOCAL): defines whether the response should be retrieved from local cache or from Convertigo server when the device can access the network. When the device has no network access, the local cache response is used.
* ttl: defines the time to live of the cached response, in milliseconds. If no value is passed, the time to live is infinite.
* enabled: allows to enable or disable the local cache on a Convertigo requestable, default value is true.

```csharp
// return the response if is already know and less than 180 sec else call the server
c8o.CallJson(".getSimpleData",
  C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 180 * 1000)
).Sync();

// same sample but with parameters, also acting as cache keys
c8o.CallJson(".getSimpleData",
  "firstname", "John",
  "lastname", "Doe",
  C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.LOCAL, 180 * 1000)
).Sync();

// make a standard network call with the server
// but in case of offline move or network failure
// return the response if is already know and less than 1 hour
c8o.CallJson(".getSimpleData",
  C8oLocalCache.PARAM, new C8oLocalCache(C8oLocalCache.Priority.SERVER, 3600 * 1000)
).Sync();
```


### Using the Full Sync ###

Full Sync enables mobile apps to handle fully disconnected scenarios, still having data handled and controlled by back end business logic. See the presentation of the Full Sync architecture for more details.

Convertigo Client SDK provides a high level access to local data following the standard Convertigo Sequence paradigm. They differ from standard sequences by a fs:// prefix. Calling these local Full Sync requestable will enable the app to read, write, query and delete data from the local database:

* fs://<database>.create creates the local database if not already exist
* fs://<database>.view queries a view from the local database
* fs://<database>.get reads an object from the local database
* fs://<database>.post writes/update an object to the local database
* fs://<database>.delete deletes an object from the local database
* fs://<database>.all gets all objects from the local database
* fs://<database>.sync synchronizes with server database
* fs://<database>.replicate_push pushes local modifications on the database server
* fs://<database>.replicate_pull gets all database server modifications
* fs://<database>.reset resets a database by removing all the data in it
* fs://<database>.put_attachment Puts (add) an attachment to a document in the database
* fs://<database>.get_attachment Gets an attachment from a document

Where fs://<database> is the name of a specific FullSync Connector in the project specified in the endpoint. The fs://<database> name is optional only if the default database name is specified with the method setDefaultDatabaseName on the C8oSetting.

An application can have many databases. On mobile (Android, iOS and Xamarin based) they are stored in the secure storage of the application. On Windows desktop application, they are stored in the user AppData/Local folder, without application isolation.

All platforms can specify a local database prefix that allows many local database copies of the same remote database. Use the method setFullSyncLocalSuffix on the C8oSetting.

```csharp
c8o.CallJson("fs://base.reset").Then((jObject, parameters) => // clear or create the "base" database
{
  // json content:
  // { "ok": true }
  return c8o.CallJson("fs://base.post", // creates a new document on "base", with 2 key/value pairs
    "firstname", "John",
    "lastname", "Doe"
  );
}).Then((jObject, parameters) =>
{
  // json content:
  // {
  //   "ok": true,
  //   "id": "6f1b52df",
  //   "rev": "1-b0620371"
  // }
  return c8o.CallJson("fs://base.get", "docid", json["id"].ToString()); // retrieves the complet document from its "docid"
}).Then((jObject, parameters) =>
{
  // json content:
  // {
  //   "lastname": "Doe",
  //   "rev": "1-b0620371",
  //   "firstname": "John",
  //   "_id": "6f1b52df"
  // }
  c8o.Log.Info(json.ToString()); // output the document in the log
  return null;
});
```

### Replicating Full Sync databases

FullSync has the ability to replicate mobile and Convertigo server databases over unreliable connections still preserving integrity. Data can be replicated in upload or download or both directions. The replication can also be continuous: a new document is instantaneously replicated to the other side.

The client SDK offers the progress event to monitor the replication progression thanks to a C8oProgress instance.

A device cannot pull private documents or push any document without authentication. A session must be established before and the Convertigo server must authenticate the session (using the Set authenticated user step for example).

```csharp
// Assuming c8o is a C8o instance properly instanciated and initiated as describe above.
c8o.CallJson(".login").Then((jObject, parameters) => // login
{
  if(jObject == "ok"){
    // replication_pull can also be sync or replication_push
    c8o.CallJson("fs://base.replication_pull").Then((jObject, parameters) => // launches a database replication from the server to the device
      {
        // json content:
        // { "ok": true }
        // the documents are retrieved from the server and can be used
        return null;
      }).Progress((progress) =>
      {
        // this code runs after each progression event
        // progress.Total is calculated and grows up then progress.Current increases to the total
        c8o.Log.Info("progress: " + progress);
      });
  }
}

```

### Replicating Full Sync databases with continuous flag ###
As mentioned above, a replication can also be continuous: a new document is instantaneously replicated to the other side.

Progress will be called at each entering replication during the all life of the application until that you explicitly cancel that one

```csharp
c8o.CallJson("fs://base.replication_pull", "continuous", true).Then((jObject, parameters) => // launches a database replication from the server to the device in continuous mode
      {
        // json content:
        // { "ok": true }
        // the documents are retrieved from the server and can be used
        return null;
      }).Progress((progress) =>
      {
        // this code runs after each progression event
        // progress.Total is calculated and grows up then progress.Current increases to the total
        c8o.Log.Info("progress: " + progress);
      });
```


### Full Sync FS_LIVE requests

Full Sync has the ability to re-execute your fs:// calls if the database is modified. The then or thenUI following a FS_LIVE parameter is re-executed after each database update. Database update can be local modification or remote modification replicated.

This allow you keep your UI synchronized with database documents.

A FS_LIVE parameter must have a string value, its liveid. The liveid allow to cancel a FS_LIVE request.


```csharp
c8o.CallJson("fs://.view",
    "ddoc", "design",
    "view", "customers",
    C8O.FS_LIVE, "customers").ThenUI((jObject, parameters) => // launches a live view
{
  // will be call now and after each database update
  UpdateCustomersUI(jObject);
  return null;
});
…
// cancel the previous FS_LIVE request, can be on application page change for example
c8o.CancelLive("customers");
```


### Full Sync Change Listener ###

Full Sync has also the ability to notify your if there is any change on the database. The progress following a FS_LIVE parameter is triggered  after each database update. The changes contains the origin of the change, and other attributes :
* isExternal
* isCurrentRevision
* isConflict
* id
* revisionId

```csharp
C8oFullSyncChangeListener changeListener = (changes) =>
{
    CheckChanges(changes);
};
…
c8o.AddFullSyncChangeListener("base", changeListener); // add this listener for the database "base" ; null or "" while use the default database.
…
c8o.RemoveFullSyncChangeListener("base", changeListener); // remove this listener for the database "base" ; null or "" while use the default database.
```