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
        private bool changed = false;
        private bool continuous = false;
        private bool finished = false;
        private bool pull = true;
        private long current = -1;
        private long total = -1;
        private string status = "";
        private string taskInfo = "";
        private object raw;

        internal C8oProgress()
        {
        }

        internal C8oProgress(C8oProgress progress)
        {
            continuous = progress.continuous;
            finished = progress.finished;
            pull = progress.pull;
            current = progress.current;
            total = progress.total;
            status = progress.status;
            taskInfo = progress.taskInfo;
            raw = progress.raw;
    }

        internal bool Changed
        {
            get
            {
                return changed;
            }

            set
            {
                changed = value;
            }
        }

        public override string ToString()
        {
            return Direction + ": " + current + "/" + total + " (" + (finished ? (continuous ? "live" : "done") : "running") + ")";
        }
        
        public bool Continuous
        {
            get
            {
                return continuous;
            }

            internal set
            {
                if (value != continuous)
                {
                    changed = true;
                    continuous = value;
                }
            }
        }

        public bool Finished
        {
            get
            {
                return finished;
            }

            internal set
            {
                if (value != finished)
                {
                    changed = true;
                    finished = value;
                }
            }
        }

        public bool Pull
        {
            get
            {
                return pull;
            }

            internal set
            {
                if (value != pull)
                {
                    changed = true;
                    pull = value;
                }
            }
        }

        public bool Push
        {
            get
            {
                return !pull;
            }
        }

        public long Current
        {
            get
            {
                return current;
            }

            internal set
            {
                if (value != current)
                {
                    changed = true;
                    current = value;
                }
            }
        }
        
        public long Total
        {
            get
            {
                return total;
            }

            internal set
            {
                if (value != total)
                {
                    changed = true;
                    total = value;
                }
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

        public string Status
        {
            get
            {
                return status;
            }

            internal set
            {
                if (!value.Equals(status))
                {
                    changed = true;
                    status = value;
                }
            }
        }

        public string TaskInfo
        {
            get
            {
                return taskInfo;
            }

            internal set
            {
                if (!value.Equals(taskInfo))
                {
                    changed = true;
                    taskInfo = value;
                }
            }
        }

        public object Raw
        {
            get
            {
                return raw;
            }

            internal set
            {
                if (value != raw)
                {
                    changed = true;
                    raw = value;
                }
            }
        }
    }
}
