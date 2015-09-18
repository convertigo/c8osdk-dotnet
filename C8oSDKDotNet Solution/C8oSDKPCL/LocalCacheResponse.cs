using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public class LocalCacheResponse
    {
        private String response;
        private String responseType;
        private long expirationDate;

        public String Response
        {
            get
            {
                return this.response;
            }
        }

        public String ResponseType
        {
            get
            {
                return this.responseType;
            }
        }

        public long ExpirationDate
        {
            get
            {
                return this.expirationDate;
            }
        }

        public LocalCacheResponse(String response, String responseType, long expirationDate = -1)
        {
            this.response = response;
            this.responseType = responseType;
            this.expirationDate = expirationDate;
        }

    }
}
