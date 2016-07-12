using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public class C8oFileTransferBase
    {
        protected int[] maxRunning = { 4 };

        public int[] MaxRunning
        {
            get { return maxRunning; }
        }

        public void Copy(C8oFileTransferSettings c8oFileTransferSettings)
        {
            maxRunning = c8oFileTransferSettings.MaxRunning;
        }
    }
}
