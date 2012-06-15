using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ObjectFilter
{
    internal class Filter
    {
        private Filter(string filter, bool isSubselect)
        {
            var parts = GetFilterParts(filter);
            ParentName = parts.Parent;
            LeafName = parts.Leaf;
            FullName = filter;
            IsSubselect = isSubselect;
        }

        public string ParentName { get; set; }
        public string LeafName { get; set; }
        public string FullName { get; set; }
        public bool IsSubselect { get; set; }

        public MatchType Match(string name)
        {
            var parts = GetFilterParts(name);

            if (IsAll && ParentName == parts.Parent)
                return MatchType.All;

            if (name == FullName)
                return MatchType.Exact;

            if (name == ParentName)
                return MatchType.Partial;

            
            if (parts.Parent == ParentName)
            {
                if (IsSubselect)
                    return MatchType.Override;

                return MatchType.Partial;
            }

            return MatchType.None;
        }

        private bool IsAll { get { return LeafName == "*"; } }
        private dynamic GetFilterParts(string filter)
        {
            dynamic parts = new ExpandoObject();
            parts.Parent = "";
            parts.Leaf = "";

            var index = filter.LastIndexOf('/');
            if (index > -1)
            {
                parts.Parent = filter.Substring(0, index);
                parts.Leaf = filter.Substring(index + 1);
            }
            else
                parts.Leaf = filter;

            return parts;
        }

        public static IEnumerable<Filter> Create(string[] filters)
        {
            var filterList = new List<Filter>();

            foreach (var filter in filters)
                filterList.AddRange(Create(filter));

            return filterList;
        }
        public static IEnumerable<Filter> Create(string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return Enumerable.Empty<Filter>();

            if (SubSelector.IsMatch(filter))
                return GetFiltersFromSubSelector(filter);

            return new List<Filter> { new Filter(filter, false) };
        }
        private static IEnumerable<Filter> GetFiltersFromSubSelector(string filter)
        {
            var match = SubSelector.Match(filter);
            var name = match.Groups["node"].Value;
            var leaves = match.Groups["leaves"].Value.Trim('(', ')').Split(',');

            var filters = new List<Filter>();
            foreach (var leaf in leaves)
                filters.Add(new Filter(string.Join("/", name, leaf), true));

            return filters;
        }
        private static Regex SubSelector = new Regex(@"^(?<node>[\w]+/?\w+)\((?<leaves>[\w|,]+)\)$");
    }
}
