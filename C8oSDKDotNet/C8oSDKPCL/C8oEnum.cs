using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.C8oEnum
{
    public class ResponseType
    {
        public static ResponseType XML = new ResponseType("pxml");
        public static ResponseType JSON = new ResponseType("json");

        private String value;

        public String Value {
            get
            {
                return this.value;
            }
        }

        public static ResponseType[] Values
        {
            get
            {
                return new ResponseType[] { XML, JSON };
            }
        }

        private ResponseType(String value)
        {
            this.value = value;
        }


        public static Boolean TryGetResponseType(String value, out ResponseType responseType) 
        {
            foreach (ResponseType rt in ResponseType.Values)
            {
                if (rt.Value.Equals(value))
                {
                    responseType = rt;
                    return true;
                }
            }
            responseType = null;
            return false;
        }

    }


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
