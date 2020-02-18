using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public interface IAuditWriter
    {
        public Task WriteRecord(object record);
    }
}
