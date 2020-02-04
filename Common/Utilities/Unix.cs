using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{

    public class Unix
    {
        public static long TimeStamp => (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds * 1000);
    }
}
