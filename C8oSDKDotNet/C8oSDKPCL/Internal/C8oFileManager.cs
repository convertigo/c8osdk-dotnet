using System;
using System.IO;

namespace Convertigo.SDK.Internal
{
    internal class C8oFileManager
    {
        /// <summary>
        /// Creates a file at the specified path and returns a file stream pointing to it.
        /// </summary>
        public Func<string, Stream> CreateFile;
        /// <summary>
        /// Returns a file stream pointing to the specified path.
        /// </summary>
        public Func<string, Stream> OpenFile;
        /// <summary>
        /// Remove the file pointing to the specified path.
        /// </summary>
        public Action<string> DeleteFile;

        public C8oFileManager(Func<string, Stream> CreateFile, Func<string, Stream> OpenFile, Action<string> DeleteFile)
        {
            this.CreateFile = CreateFile;
            this.OpenFile = OpenFile;
            this.DeleteFile = DeleteFile;
        }
    }
}
