namespace Convertigo.SDK
{
    public class C8oFileTransferBase
    {
        protected string projectName = "lib_FileTransfer";
        protected string taskDb = "c8ofiletransfer_tasks";
        protected int maxRunning = 4;

        public string ProjectName
        {
            get { return projectName; }
        }

        public string TaskDb
        {
            get { return taskDb; }
        }

        public int MaxRunning
        {
            get { return maxRunning; }
        }

        public void Copy(C8oFileTransferSettings settings)
        {
            projectName = settings.projectName;
            taskDb = settings.taskDb;
            maxRunning = settings.MaxRunning;
        }
    }
}
