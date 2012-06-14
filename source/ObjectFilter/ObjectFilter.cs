using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ObjectFilter
{
    public class ObjectFilter
    {
        private object _source;
        private IEnumerable<Filter> _filters;
        private List<XElement> _elementsToDelete;

        public ObjectFilter(object source, string[] filters)
        {
            _source = source;
            _filters = Filter.Create(filters);
        }

        /// <summary>
        /// Processes the filter
        /// </summary>
        /// <returns></returns>
        public XDocument Process()
        {
            _elementsToDelete = new List<XElement>();

            var xmlDoc = GetObjectXml(_source);

            foreach (var node in xmlDoc.Root.Elements())
                Process(null, node);

            foreach (var el in _elementsToDelete)
                el.Remove();

            return xmlDoc;
        }

        public T Process<T>()
        {
            var result = Process();
            var serializer = new XmlSerializer(typeof(T));
            using (var rdr = result.CreateReader())
                return (T)serializer.Deserialize(rdr);
        }

        private void Process(string parentName, XElement node)
        {
            var currentName = GetParentName(parentName, node);

            var matchResult = IsMatch(currentName);
            if(matchResult.HasValue && matchResult.Value)
                return; //We have a match

            //We may have a partial match
            if (node.HasElements && matchResult.HasValue)
            {
                foreach (var child in node.Elements())
                {
                    Process(currentName, child);
                }
            }
            else //We either have no match or an override
                _elementsToDelete.Add(node);
        }

        private bool? IsMatch(string name)
        {
            var matchingFilters = _filters.Select(x => x.Match(name)).Where(x => x != MatchType.None).ToList();

            //No matches so move on
            if (!matchingFilters.Any())
                return false;

            //We've got an exact match so we're good
            if (matchingFilters.Any(x => x == MatchType.Exact))
                return true;

            //We have a partial match, so let's keep checking.
            //It means we're a node that has children and we have a more specific filter 
            if (matchingFilters.Any(x => x == MatchType.Partial))
                return false;

            //We have a subselect filter and we have a match on the parent
            //But we don't want the * filter to grab it, so we need to get out.
            //This happens when a * and subselect are combined: *,a/b(c,d)
            if (matchingFilters.Any(x => x == MatchType.Override))
                return null;

            return true;
        }
        private string GetParentName(string currentParentName, XElement node)
        {
            if (currentParentName == null)
                return node.Name.ToString();

            return currentParentName + "/" + node.Name;
        }

        private static XDocument GetObjectXml(object obj)
        {
            var serializer = new XmlSerializer(obj.GetType());

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, obj);
                stream.Position = 0;
                return XDocument.Load(stream);
            }
        }
    }
}
