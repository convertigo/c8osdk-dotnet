using System;
using System.Net.NetworkInformation;

namespace Convertigo.SDK.C8oEnum
{
    internal class LocalCachePolicy
    {
        public static LocalCachePolicy PRIORITY_SERVER = new LocalCachePolicy("priority-server", () =>
        {
            Boolean networkIsAvailable = NetworkInterface.GetIsNetworkAvailable();
            return !networkIsAvailable;
        });

        public static LocalCachePolicy PRIORITY_LOCAL = new LocalCachePolicy("priority-local", () =>
        {
            return true;
        });

        private String value;
        public Func<Boolean> IsAvailable;

        public String Value
        {
            get
            {
                return this.value;
            }
        }

        public static LocalCachePolicy[] Values
        {
            get
            {
                return new LocalCachePolicy[] { PRIORITY_SERVER, PRIORITY_LOCAL };
            }
        }

        private LocalCachePolicy(String value, Func<Boolean> isAvailable)
        {
            this.value = value;
            this.IsAvailable = isAvailable;
        }

        internal static Boolean TryGetLocalCachePolicy(String localCachePolicyStr, out LocalCachePolicy localCachePolicy)
        {
            LocalCachePolicy[] values = LocalCachePolicy.Values;
            foreach (LocalCachePolicy value in values)
            {
                if (value.value.Equals(localCachePolicyStr))
                {
                    localCachePolicy = value;
                    return true;
                }
            }
            localCachePolicy = null;
            return false;
        }

    }
}
