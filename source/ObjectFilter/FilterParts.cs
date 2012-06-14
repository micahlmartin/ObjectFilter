using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectFilter
{
    internal class FilterParts
    {
        public FilterParts(string filter)
        {
            var parts = filter.Split('/');
            ParentName = string.Join("/", parts.Take(parts.Length - 1));
            LeafName = parts[parts.Length - 1];
        }

        public string ParentName { get; set; }
        public string LeafName { get; set; }
        public bool IsAll { get { return LeafName == "*"; } }
    }
}
