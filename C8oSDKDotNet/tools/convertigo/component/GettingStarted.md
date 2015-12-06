# Getting Started with Convertigo MBaaS SDK
## This is the way it works ##
1. Create a Convertigo End Point:

	using Convertigo.SDK;
    
    C8o myC8o = new C8o("http://my_convertigo_server/convertigo/projects/my_convertigo_project");
2. Use it:

    JObject data = myC8o.CallJSON(".my_sequence",
		key1, value1,
		key2, value2
	).sync();

