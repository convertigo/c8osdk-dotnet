using System;

namespace Convertigo.SDK
{
    public class C8oFileTransferBase
    {
        protected string projectName = "lib_FileTransfer";
        protected string taskDb = "c8ofiletransfer_tasks";
        protected string filePrefix = "";
        protected TimeSpan maxDurationForTransferAttempt = TimeSpan.FromMinutes(40);
        protected TimeSpan maxDurationForChunk = TimeSpan.FromMinutes(10);
        protected int maxRunning = 4;
        protected bool useCouchBaseReplication = true;
        protected int maxParallelChunkDownload = 2;

        public string ProjectName
        {
            get { return projectName; }
        }

        public string TaskDb
        {
            get { return taskDb; }
        }

        public string FilePrefix
        {
            get { return filePrefix; }
        }

        public int MaxRunning
        {
            get { return maxRunning; }
        }

        public TimeSpan MaxDurationForTransferAttempt
        {
            get { return maxDurationForTransferAttempt; }
        }

        public TimeSpan MaxDurationForChunk
        {
            get { return maxDurationForChunk; }
        }

        public bool UseCouchBaseReplication
        {
            get { return useCouchBaseReplication; }
        }

        public int MaxParallelChunkDownload
        {
            get { return maxParallelChunkDownload; }
        }

        public void Copy(C8oFileTransferSettings settings)
        {
            projectName = settings.projectName;
            taskDb = settings.taskDb;
            filePrefix = settings.filePrefix;
            maxRunning = settings.maxRunning;
            maxDurationForTransferAttempt = settings.maxDurationForTransferAttempt;
            maxDurationForChunk = settings.maxDurationForChunk;
            useCouchBaseReplication = settings.useCouchBaseReplication;
            maxParallelChunkDownload = settings.maxParallelChunkDownload;
        }
    }
}
