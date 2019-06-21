using System;

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

        public C8oFileTransferSettings SetProjectName(string projectName)
        {
            if (projectName != null)
            {
                this.projectName = projectName;
            }
            return this;
        }

        public C8oFileTransferSettings SetTaskDb(string taskDb)
        {
            if (taskDb != null)
            {
                this.taskDb = taskDb;
            }
            return this;
        }

        public C8oFileTransferSettings SetFilePrefix(string filePrefix)
        {
            if (filePrefix != null)
            {
                this.filePrefix = filePrefix;
            }
            return this;
        }

        public C8oFileTransferSettings SetMaxRunning(int maxRunning)
        {
            if (maxRunning > 0)
            {
                this.maxRunning = maxRunning;
            }
            return this;
        }

        public C8oFileTransferSettings SetMaxDurationForTransferAttempt(TimeSpan maxDurationForTransferAttempt)
        {
            this.maxDurationForTransferAttempt = maxDurationForTransferAttempt;
            return this;
        }

        public C8oFileTransferSettings SetMaxDurationForChunk(TimeSpan maxDurationForChunk)
        {
            this.maxDurationForChunk = maxDurationForChunk;
            return this;
        }

        public C8oFileTransferSettings SetUseCouchBaseReplication(bool useCouchBaseReplication)
        {
            this.useCouchBaseReplication = useCouchBaseReplication;
            return this;
        }

        public C8oFileTransferSettings SetMaxParallelChunkDownload(int maxParallelChunkDownload)
        {
            this.maxParallelChunkDownload = maxParallelChunkDownload;
            return this;
        }

    }
}
