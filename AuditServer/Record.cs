using System;
using System.Collections.Generic;
using System.Text;

namespace AuditServer
{
    public class Record
    {
        public DateTime RecordTime { get; }

        public string Message { get; set; }


        public Record()
        {
            // Names are case sensitive
            RecordTime = DateTime.Now;
        }
    }
}
