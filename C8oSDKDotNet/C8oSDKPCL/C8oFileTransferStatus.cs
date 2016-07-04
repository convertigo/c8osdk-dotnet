namespace Convertigo.SDK
{
    public class C8oFileTransferStatus
    {
        public static readonly C8oFileTransferState StateQueued = new C8oFileTransferState("queued");
        
        public static readonly C8oFileTransferState StateAuthenticated = new C8oFileTransferState("authenticated");
        public static readonly C8oFileTransferState StateSplitting = new C8oFileTransferState("splitting");
        public static readonly C8oFileTransferState StateReplicate = new C8oFileTransferState("replicating");
        public static readonly C8oFileTransferState StateAssembling = new C8oFileTransferState("assembling");
        public static readonly C8oFileTransferState StateCleaning = new C8oFileTransferState("cleaning");
        public static readonly C8oFileTransferState StateFinished = new C8oFileTransferState("finished");

        public class C8oFileTransferState
        {
            string toString;

            internal C8oFileTransferState(string toString)
            {
                this.toString = toString;
            }

            public override string ToString()
            {
                return toString;
            }
        }

        private C8oFileTransferState state = StateQueued;

        public C8oFileTransferState State
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

        private string uuid;

        public string Uuid
        {
            get
            {
                return uuid;
            }
        }

        private string filepath;

        public string Filepath
        {
            get
            {
                return filepath;
            }
        }

        private string serverFilepath;

        public string ServerFilepath
        {
            get
            {
                return serverFilepath;
            }
            internal set
            {
                serverFilepath = value;
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

        private bool isDownload;

        public bool IsDownload
        {
            get
            {
                return isDownload;
            }
            set
            {
                isDownload = value;
                if (value)
                {
                    tot();
                }
            }
        }

        internal C8oFileTransferStatus(string uuid, string filepath)
        {
            this.uuid = uuid;
            this.filepath = filepath;
            total = 0;
            //total = int.Parse(uuid.Substring(uuid.LastIndexOf('-') + 1));
        }
        private void tot()
        {
            total = int.Parse(uuid.Substring(uuid.LastIndexOf('-') + 1));
        }
    }
}
