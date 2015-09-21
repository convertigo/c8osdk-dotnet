using Convertigo.SDK.C8oEnum;
using Convertigo.SDK.Utils;
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
        private ResponseType responseType;
        private long expirationDate;

        public String Response
        {
            get
            {
                return this.response;
            }
        }

        public ResponseType ResponseType
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

        public LocalCacheResponse(String response, ResponseType responseType, long expirationDate = -1)
        {
            this.response = response;
            this.responseType = responseType;
            this.expirationDate = expirationDate;
        }

        public Boolean Expired()
        {
            if (this.expirationDate <= 0)
            {
                return false;
            }
            else
            {
                long currentDate = C8oUtils.GetUnixEpochTime(DateTime.Now);
                return (this.expirationDate < currentDate);
            }
        }

    }
}
