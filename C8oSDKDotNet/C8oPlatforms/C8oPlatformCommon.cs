using System.IO;

namespace Convertigo.SDK.Internal
{
    public class C8oPlatformCommon
    {
        static public void Init()
        {
            C8oFileTransfer.fileManager = new C8oFileManager(path =>
            {
                FileStream fileStream = File.Create(path);
                return fileStream;
            }, path =>
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                return fileStream;
            });

            C8oFullSyncCbl.Init();
            C8oHttpInterfaceSSL.Init();
        }
    }
}
