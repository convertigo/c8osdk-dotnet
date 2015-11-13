using Convertigo.SDK.FullSync;
using System;
using System.Collections.Generic;
using System.Text;

namespace Convertigo.SDK
{
    public static class C8oFullSyncMobileExtension
    {
        public static FullSyncMobile GetFullSyncMobile(this C8o c8o)
        {
            return new FullSyncMobile();
        }
    }
}
