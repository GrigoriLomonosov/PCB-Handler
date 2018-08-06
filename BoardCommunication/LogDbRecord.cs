using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardCommunication
{
    /// <summary>
    /// Represents a record in the database of logs
    /// </summary>
    class LogDbRecord
    {
        public int id
        {
            get;
        }

        public DateTime date
        {
            get;
        }

        public int node
        {
            get;
        }

        public int _event
        {
            get;
        }

        public string time
        {
            get;
        }

        public int tag
        {
            get;
        }

        public LogDbRecord(int id, DateTime date, int node, int _event, string time, int tag)
        {
            this.id = id;
            this.date = date;
            this.node = node;
            this._event = _event;
            this.time = time;
            this.tag = tag;
        }
    }
}
