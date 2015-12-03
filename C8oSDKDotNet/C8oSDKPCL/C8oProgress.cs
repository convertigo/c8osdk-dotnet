using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public class C8oProgress
    {
        internal bool finished;
        internal bool pull;
        internal long current;
        internal long total;
        internal string taskInfo;
        internal string status;
        internal object raw;

        internal C8oProgress()
        {
        }

        public override string ToString()
        {
            return "" + current + "/" + total + " (" + (finished ? "done" : "running") + ")";
        }

        public long Current
        {
            get
            {
                return current;
            }
        }
        
        public long Total
        {
            get
            {
                return total;
            }
        }

        public string Direction
        {
            get
            {
                return pull ?
                C8oFullSyncTranslator.FULL_SYNC_RESPONSE_VALUE_DIRECTION_PULL :
                C8oFullSyncTranslator.FULL_SYNC_RESPONSE_VALUE_DIRECTION_PUSH;
            }
        }

        public string TaskInfo
        {
            get
            {
                return taskInfo;
            }
        }

        public string Status
        {
            get
            {
                return status;
            }
        }

        public bool Finished
        {
            get
            {
                return finished;
            }
        }

        public bool Pull
        {
            get
            {
                return pull;
            }
        }

        public bool Push
        {
            get
            {
                return !pull;
            }
        }

        public object Raw
        {
            get
            {
                return raw;
            }
        }
    }
}
