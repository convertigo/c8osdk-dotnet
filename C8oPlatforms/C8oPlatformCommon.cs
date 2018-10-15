using System;
using System.IO;

namespace Convertigo.SDK.Internal
{
    public class C8oPlatformCommon
    {
        static public void Init()
        {
            C8oFileTransfer.fileManager = new C8oFileManager(path =>
            {
                if (File.Exists(path))
                {
                    FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
                    return fileStream;
                }
                else
                {
                    FileStream fileStream = File.Create(path);
                    return fileStream;
                }
            }, path =>
            {
                FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                return fileStream;
            }, path =>
            {
                File.Delete(path);
            }, (source, dest) =>
            {
                File.Move(source, dest);
            });

            C8oFullSyncCbl.Init();
            C8oHttpInterfaceSSL.Init();

            try
            {
                Couchbase.Lite.Storage.SystemSQLite.Plugin.Register();
            }
            catch
            {

            }
#if !UWP
            try
            {
                Couchbase.Lite.Storage.ForestDB.Plugin.Register();
            }
            catch
            {

            }
#endif
        }
    }
}
