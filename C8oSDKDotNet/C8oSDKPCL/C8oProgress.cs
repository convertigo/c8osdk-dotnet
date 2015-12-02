using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    public class C8oProgress
    {
        private long current;
        private long total;
        private string direction;
        private string taskInfo;
        private string status;

        internal C8oProgress(JObject json)
        {
            current = json["current"].Value<long>();
            total = json["total"].Value<long>();
            direction = json["direction"].Value<string>();
            taskInfo = json["taskInfo"].Value<string>();
            status = json["status"].Value<string>();
        }

        public long Current
        {
            get
            {
                return current;
            }
        }
        
        public long Total
        {
            get
            {
                return total;
            }
        }

        public string Direction
        {
            get
            {
                return direction;
            }
        }

        public string TaskInfo
        {
            get
            {
                return taskInfo;
            }
        }

        public string Status
        {
            get
            {
                return status;
            }
        }

        public bool Stopped
        {
            get
            {
                return "Stopped".Equals(status);
            }
        }
    }
}
