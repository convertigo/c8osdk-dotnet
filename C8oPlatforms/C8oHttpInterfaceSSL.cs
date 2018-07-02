using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Convertigo.SDK.Internal
{
    class C8oHttpInterfaceSSL : C8oHttpInterface
    {
        static IDictionary<HttpWebRequest, bool> requests = new Dictionary<HttpWebRequest, bool>();

        static C8oHttpInterfaceSSL()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, error) =>
            {
                lock (requests)
                {
                    if (requests.ContainsKey(sender as HttpWebRequest))
                    {
                        if (requests[sender as HttpWebRequest])
                        {
                            return true;
                        }
                        else
                        {
                            return chain.ChainStatus.Length == 0;
                        }
                    }
                }
                return false;
            };
        }

        private List<X509Certificate2> clientCertificates = null;

        public C8oHttpInterfaceSSL(C8o c8o)
            : base(c8o)
        {
            if (clientCertificates == null && (c8o.ClientCertificateBinaries != null || c8o.ClientCertificateFiles != null))
            {
                clientCertificates = new List<X509Certificate2>();
            }

            if (c8o.ClientCertificateBinaries != null)
            {
                foreach (var entry in c8o.ClientCertificateBinaries)
                {
                    try
                    {
                        clientCertificates.Add(new X509Certificate2(entry.Key, entry.Value));
                    }
                        catch (Exception e)
                    {
                        e.ToString();
                    }
            }
            }

            if (c8o.ClientCertificateFiles != null)
            {
                foreach (var entry in c8o.ClientCertificateFiles)
                {
                    try
                    {
                        clientCertificates.Add(new X509Certificate2(entry.Key, entry.Value));
                    }
                    catch (Exception e)
                    {
                        e.ToString();
                    }
                }
            }
        }

        internal override void OnRequestCreate(HttpWebRequest request)
        {
            request.UserAgent = "Convertigo Client SDK " + C8o.GetSdkVersion();

            if (clientCertificates != null)
            {
                foreach (var cert in clientCertificates)
                {
                    request.ClientCertificates.Add(cert);
                }
            }
            lock (requests)
            {
                if (requests.Count > 10)
                {
                    var toRemove = new List<HttpWebRequest>();
                    foreach (var entry in requests)
                    {
                        if (entry.Key.HaveResponse)
                        {
                            toRemove.Add(entry.Key);
                        }
                    }
                    foreach (var item in toRemove)
                    {
                        requests.Remove(item);
                    }
                }

                if (request.Address.Scheme.Equals("https"))
                {
                    requests[request] = c8o.TrustAllCetificates;
                }
            }
        }

        public static void Init()
        {
            System.Net.ServicePointManager.Expect100Continue = false;
            C8o.C8oHttpInterfaceUsed = Type.GetType("Convertigo.SDK.Internal.C8oHttpInterfaceSSL", true);
        }
    }
}
