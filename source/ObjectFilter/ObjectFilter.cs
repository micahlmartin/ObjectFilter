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
            _filters = GetFilters(filters);
        }

        private IEnumerable<Filter> GetFilters(string[] filters)
        {
            return filters.Select(x => new Filter(x)).ToList();
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

            if (IsMatch(currentName))
                return;

            if (node.HasElements)
            {
                foreach (var child in node.Elements())
                {
                    Process(currentName, child);
                }
            }
            else
                _elementsToDelete.Add(node);
        }

        private bool IsMatch(string name)
        {
            var filterParts = new FilterParts(name);
            return _filters.Any(x => x.IsMatch(filterParts));
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
