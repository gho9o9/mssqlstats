using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mssqlstats
{
    class MssqlStatsException : Exception
    {
        public MssqlStatsException(string message) : base(message)
        {
        }
    }
}
