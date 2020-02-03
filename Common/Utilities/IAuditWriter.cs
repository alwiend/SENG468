using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    public interface IAuditWriter
    {
        public string WriteRecord(object record);
    }
}
