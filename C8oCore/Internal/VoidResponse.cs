namespace Convertigo.SDK.Internal
{
    /// <summary>
    /// Represents a void response in case of c8o call which returns nothing directly.
    /// </summary>
    internal class VoidResponse
    {
        private static readonly VoidResponse VOID_RESPONSE_INSTANCE = new VoidResponse();
	
	    private VoidResponse() {}
	
	    public static VoidResponse GetInstance() {
		    return VoidResponse.VOID_RESPONSE_INSTANCE;
	    }
    }
}