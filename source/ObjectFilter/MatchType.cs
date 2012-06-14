using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectFilter
{
    internal enum MatchType
    {
        None = -1,
        All = 0,
        Partial = 1,
        Exact = 2,
        Override = 3
    }
}
