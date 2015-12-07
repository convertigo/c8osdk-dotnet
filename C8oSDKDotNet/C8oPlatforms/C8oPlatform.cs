using System;
using System.Collections.Generic;
using System.Text;
namespace Convertigo.SDK
{
    /// <summary>
    /// Platform initialization class. For shared projects the calls must be done in the platform specific code (iOS,  Android). 
    /// </summary>
    public class C8oPlatform
    {
        /// <summary>
        /// Init() must be called prior to any other call to Convertigo SDK. This will initialize all the Convertigo SDK Extensions such as FullSync or Local Cache.
        /// </summary>
        static public void Init()
        {
            C8oFullSyncCbl.Init();
        }
    }
}
