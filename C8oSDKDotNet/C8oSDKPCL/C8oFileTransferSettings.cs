using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public class C8oFileTransferSettings : C8oFileTransferBase
    {
         

        public C8oFileTransferSettings()
        {

        }

        public C8oFileTransferSettings(C8oFileTransferSettings c8oFileTransferSettings)
        {
            Copy(c8oFileTransferSettings);
        }

        public C8oFileTransferSettings Clone()
        {
            return new C8oFileTransferSettings(this);
        }


        public C8oFileTransferSettings SetMaxRunning(int maxRunning)
        {
            if(maxRunning <= 0 || maxRunning > 4)
            {
                throw new C8oException("maxRunning must be between 1 and 4");
            }
            else
            {
                this.maxRunning[0] = maxRunning;
            }
            return this;
        }
    }
}
