using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.C8oEnum
{
    internal class LocalCachePolicy
    {
        public static LocalCachePolicy PRIORITY_SERVER = new LocalCachePolicy("priority-server", () =>
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                return true;
            }
            else
            {
                return false;
            }
        });

        public static LocalCachePolicy PRIORITY_LOCAL = new LocalCachePolicy("priority-server", () =>
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

        internal static LocalCachePolicy GetLocalCachePolicy(String localCachePolicyStr)
        {
            LocalCachePolicy[] values = LocalCachePolicy.Values;
            foreach (LocalCachePolicy value in values)
            {
                if (value.Equals(localCachePolicyStr))
                {
                    return value;
                }
            }
            return null;
        }

    }
}
