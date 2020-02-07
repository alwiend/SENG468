using System;
using System.Threading;

namespace WebServer
{
    public class GlobalTransaction
    {
        private int _count = 0;
        public int Count
        {
            get
            {
                return Interlocked.Increment(ref _count);
            }
        }
    }
}
