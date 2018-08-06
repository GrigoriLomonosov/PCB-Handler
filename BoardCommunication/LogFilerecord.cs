using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardCommunication
{
    /// <summary>
    /// Represents a record in the processes log data file
    /// </summary>
    class LogFilerecord
    {
        public string node
        {
            get; set;
        }

        public string callTime
        {
            get; set;
        }

        public string callDate
        {
            get; set;
        }

        public string cancelTime
        {
            get; set;
        }

        public string cancelDate
        {
            get; set;
        }

        public string onTime
        {
            get; set;
        }

        public string offTime
        {
            get; set;
        }

        public string cancelKey
        {
            get; set;
        }

        public LogFilerecord(string node, string callTime, string callDate, string cancelTime, string cancelDate, string onTime, string offTime, string cancelKey)
        {
            this.node = node;
            this.callTime = callTime;
            this.callDate = callDate;
            this.cancelTime = cancelTime;
            this.cancelDate = cancelDate;
            this.onTime = onTime;
            this.offTime = offTime;
            this.cancelKey = cancelKey;
        }
    }
}
