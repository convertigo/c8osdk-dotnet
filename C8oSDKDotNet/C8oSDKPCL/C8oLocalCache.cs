using System;

namespace Convertigo.SDK
{
    /// <summary>
    /// A C8oLocalCache object is used to configure the local cache feature for "C8o.Call*" methods.
    /// The key of this parameter must be C8oLocalCache.PARAM and the value a new C8oLocalCache instance.
    /// 
    /// Sometimes we would like to use local cache on C8O calls and responses, in order to:
    ///  * save network traffic between the device and the server,
    ///  * be able to display data when the device is not connected to the network.
    /// The Local Cache feature allows to store locally on the device the responses to a C8O call, using the variables and their values as cache key.
    /// </summary>
    public class C8oLocalCache
    {
        public readonly static string PARAM = "__localCache";

        public class Priority
        {
            public static readonly Priority SERVER = new Priority(c8o =>
            {
                return true;
            });

            public static readonly Priority LOCAL = new Priority(c8o =>
            {
                return true;
            });

            internal Func<C8o, bool> IsAvailable;

            private Priority(Func<C8o, bool> isAvailable)
            {
                IsAvailable = isAvailable;
            }
        }

        internal Priority priority;
        internal long ttl;
        internal bool enabled;

        /// <summary>
        /// Should be use as a value for "c8o.Call*", with a C8oLocalCache.PARAM key. The local 
        /// </summary>
        /// <param name="priority">Defines whether the response should be retrieved from LOCAL cache
        /// or from Convertigo SERVER when the device can access the network
        /// when the device has no network access, the local cache response is used, if existing and not expired
        /// </param>
        /// <param name="ttl">
        /// The time-to-live in ms. Defines the time to live of the cached response, in milliseconds
        /// if no value is passed (or no positive value), the time to live is infinite
        /// </param>
        /// <param name="enabled">
        /// Allows to enable or disable the local cache on a Convertigo requestable, default value is true
        /// </param>
        public C8oLocalCache(Priority priority, long ttl = -1, bool enabled = true)
        {
            if (priority == null)
            {
                throw new System.ArgumentException("Local Cache priority cannot be null");
            }
            this.priority = priority;
            this.ttl = ttl;
            this.enabled = enabled;
        }
    }
}
