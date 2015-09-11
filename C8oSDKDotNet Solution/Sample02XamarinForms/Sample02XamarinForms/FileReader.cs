using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample02XamarinForms
{
    public class FileReader
    {
        public Func<String, byte[]> ReadFile;

        public FileReader(Func<String, byte[]> readFile)
        {
            this.ReadFile = readFile;
        }
    }
}
