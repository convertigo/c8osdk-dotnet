using Convertigo.SDK.Internal;
using System;

namespace Convertigo.SDK
{
    internal class C8oLocalCacheResponse
    {
        private string response;
        private string responseType;
        private long expirationDate;

        public C8oLocalCacheResponse(string response, string responseType, long expirationDate)
        {
            this.response = response;
            this.responseType = responseType;
            this.expirationDate = expirationDate;
        }

        public bool Expired
        {
            get
            {
                if (expirationDate <= 0)
                {
                    return false;
                }
                else
                {
                    long currentDate = C8oUtils.GetUnixEpochTime(DateTime.Now);
                    return expirationDate < currentDate;
                }
            }
        }

        public string Response
        {
            get
            {
                return response;
            }
        }

        public string ResponseType
        {
            get
            {
                return responseType;
            }
        }

        public long ExpirationDate
        {
            get
            {
                return expirationDate;
            }
        }
    }
}
