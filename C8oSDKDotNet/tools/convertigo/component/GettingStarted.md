# Getting Started with Convertigo MBaaS SDK
The SDK uses a C8o object to access the Convertigo Server. Here are the steps to call a Service fomr the server.
## Create a Convertigo End Point: ##
Assuming **myConvertigoServer** the name of a running  Convertigo server and **myConvertigoProject** is a deployed Convertigo project in this server. Use this to create a C8o object in your app:

	using Convertigo.SDK;
    
    C8o myC8o = new C8o("http://myConvertigoServer/convertigo/projects/myConvertigoProject");


## Use it:##
To call a service in the server just use the CallJSON method  passing the reference to the service you want to call. A service reference is also called a "requestable" is in this form :
> project.service

As the project is specified in the end point we just have to call the service of the default project. In this case the **mySequence** service. You can pass any key/value pair to the call . They will be automatically mapped to sequence variables on the server. 


    JObject data = myC8o.CallJSON(".mySequence",
		key1, value1,
		key2, value2
	).sync();


CallJSON is asynchronous and returns a **promise** object you can use the **Sync()** method to wait for the server response. Response is a JObject containing a the server data.

## Full Convertigo SDK Documentation ##
You can get all the details SDK documentations here:

[http://www.convertigo.com/document/next/7-4-0/reference-manual/convertigo-sdk/](http://www.convertigo.com/document/next/7-4-0/reference-manual/convertigo-sdk/ "SDK Documentation")

