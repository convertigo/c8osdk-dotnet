using System;
using System.Collections.Generic;
using System.Text;

namespace BigFileTransfer2
{
    public class FileManager
    {
        // File path, file data 
        public Func<String, byte[]> ReadFile;

        public FileManager(Func<String, byte[]> ReadFile)
        {
            this.ReadFile = ReadFile;
        }


    }
}
