using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C8oBigFileTransfer
{
    public class DownloadStatus
    {
        public static readonly DownloadState StateQueued = new DownloadState("queued");
        public static readonly DownloadState StateAuthenticated = new DownloadState("authenticated");
        public static readonly DownloadState StateReplicate = new DownloadState("replicating");
        public static readonly DownloadState StateAssembling = new DownloadState("assembling");
        public static readonly DownloadState StateCleaning = new DownloadState("cleaning");
        public static readonly DownloadState StateFinished = new DownloadState("finished");
        
        public class DownloadState
        {
            String toString;

            internal DownloadState(String toString)
            {
                this.toString = toString;
            }

            public override string ToString()
            {
                return toString;
            }
        }

        private DownloadState state = StateQueued;

        public DownloadState State
        {
            get
            {
                return state;
            }
            internal set
            {
                state = value;
            }
        }

        private String uuid;

        public String Uuid
        {
            get
            {
                return uuid;
            }
        }

        private String filepath;

        public String Filepath
        {
            get
            {
                return filepath;
            }
        }

        public int current;

        public int Current
        {
            get
            {
                return current;
            }

            internal set
            {
                current = value;
            }
        }

        public int total;

        public int Total
        {
            get
            {
                return total;
            }
        }

        public double Progress
        {
            get
            {
                return total > 0 ? current * 1.0f / total : 0;
            }
        }

        internal DownloadStatus(String uuid, String filepath)
        {
            this.uuid = uuid;
            this.filepath = filepath;
            this.total = int.Parse(uuid.Substring(uuid.LastIndexOf('-') + 1));
        }
    }
}
