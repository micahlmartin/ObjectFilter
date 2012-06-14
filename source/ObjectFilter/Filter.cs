using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ObjectFilter
{
    internal class Filter
    {
        private string _Originalfilter;
        private IList<FilterParts> _expandedFilters = new List<FilterParts>();

        public Filter(string filter)
        {
            _Originalfilter = filter;

            if (SubSelector.IsMatch(filter))
                ProcessSubSelector(filter);
            else
                _expandedFilters.Add(new FilterParts(filter));
        }

        public bool IsMatch(FilterParts filterParts)
        {
            return _expandedFilters.Any(x => x.ParentName == filterParts.ParentName && (x.LeafName == filterParts.LeafName || x.IsAll));
        }

        private FilterParts GetMatchFilterParts(string name)
        {
            FilterParts filterParts;
            if (!_matchFilterPartsCache.TryGetValue(name, out filterParts))
            {
                filterParts = new FilterParts(name);
                _matchFilterPartsCache.Add(name, filterParts);
            }
            return filterParts;
        }
        private void ProcessSubSelector(string filter)
        {
            var match = SubSelector.Match(filter);
            var name = match.Groups["node"].Value;
            var leaves = match.Groups["leaves"].Value.Trim('(', ')').Split(',');

            foreach (var leaf in leaves)
                _expandedFilters.Add(new FilterParts(string.Join("/", name, leaf)));
        }
        private IDictionary<string, FilterParts> _matchFilterPartsCache = new Dictionary<string, FilterParts>();
        private static Regex SubSelector = new Regex(@"^(?<node>[\w]+/?\w+)\((?<leaves>[\w|,]+)\)$");
    }
}
