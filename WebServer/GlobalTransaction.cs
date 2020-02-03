using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebServer
{
    public class GlobalTransaction
    {
        private int _count = 0;
        public int Count
        {
            get
            {
                return ++_count;
            }
        }
    }
}
