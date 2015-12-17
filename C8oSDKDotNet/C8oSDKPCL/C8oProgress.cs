using Convertigo.SDK.Internal;

namespace Convertigo.SDK
{
    /// <summary>
    /// This class gives dome information about a running replication
    /// </summary>
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

        /// <summary>
        /// A built in replication status indicator.
        /// </summary>
        /// <returns>A String in the form "Direction : current / total (running)"</returns>
        public override string ToString()
        {
            return Direction + ": " + current + "/" + total + " (" + (finished ? (continuous ? "live" : "done") : "running") + ")";
        }

        /// <summary>
        /// true if in continuous mode, false otherwise. In continuous mode, replications are done continuously as long as
        /// the network is present. Otherwise replication stops when all the documents have been replicated
        /// </summary>
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

        /// <summary>
        /// For a normal repliacation will be true when the replication has finished. For a continuous replication, will be true during phase 1
        /// when all documents are being replicate to a stable state, then false during the continuous replication process. As design documents
        /// are also replicated during a database sync, never use a view before the progress.finished == true to be sure the design document holding
        /// this view is replicated locally.
        /// </summary>
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

        /// <summary>
        /// True is the replication is in pull mode (From server to device) false in push mode (Mobile to server)
        /// </summary>
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

        /// <summary>
        /// True is the replication is in push mode (From mobile to device) false in pull  mode (Server to mobile)
        /// </summary>
        public bool Push
        {
            get
            {
                return !pull;
            }
        }

        /// <summary>
        /// The current number of revisions repliacted. can be used as a progress indicator.
        /// </summary>
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

        /// <summary>
        /// The total number of revisions to be repliacted so far.
        /// </summary>
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

        /// <summary>
        /// A Direction (Pull or push) information
        /// </summary>
        public string Direction
        {
            get
            {
                return pull ?
                C8oFullSyncTranslator.FULL_SYNC_RESPONSE_VALUE_DIRECTION_PULL :
                C8oFullSyncTranslator.FULL_SYNC_RESPONSE_VALUE_DIRECTION_PUSH;
            }
        }

        /// <summary>
        /// A Status information
        /// </summary>
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

        /// <summary>
        /// A task information status from the underlying replication engine.
        /// </summary>
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

        /// <summary>
        /// The underlying replication engine replication object.
        /// </summary>
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
