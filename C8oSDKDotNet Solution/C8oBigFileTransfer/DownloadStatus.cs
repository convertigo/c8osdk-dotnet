using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C8oBigFileTransfer
{
    public class DownloadStatus
    {

        public int Current;

        public int Total;

        public DownloadStatus(int current, int total)
        {
            this.Current = current;
            this.Total = total;
        }

    }
}
