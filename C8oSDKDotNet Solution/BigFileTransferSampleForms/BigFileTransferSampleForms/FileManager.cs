using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BigFileTransfer
{
    public class FileManager
    {
        /// <summary>
        /// Creates a file at the specified path and returns a file stream pointing to it.
        /// </summary>
        public Func<String, Stream> CreateFile;
        /// <summary>
        /// Returns a file stream pointing to the specified path.
        /// </summary>
        public Func<String, Stream> OpenFile;

        public FileManager(Func<String, Stream> CreateFile, Func<String, Stream> OpenFile)
        {
            this.CreateFile = CreateFile;
            this.OpenFile = OpenFile;
        }


    }
}
