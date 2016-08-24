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

        public C8oFileTransferSettings SetMaxRunning(int maxRunning)
        {
            if (maxRunning > 0)
            {
                this.maxRunning = maxRunning;
            }
            return this;
        }
    }
}
