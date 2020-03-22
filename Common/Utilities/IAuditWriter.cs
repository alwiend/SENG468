using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public interface IAuditWriter : IDisposable
    {
        public void WriteRecord(object record);
    }
}
