using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ObjectFilter
{
    public class FilterProcessor
    {
        private object _source;
        private IEnumerable<Filter> _filters;

        public FilterProcessor(object source, IEnumerable<string> filters)
        {
            _source = source;
            _filters = Filter.Create(filters);
        }

        /// <summary>
        /// Filters the object
        /// </summary>
        /// <returns>A filtered instance of the object</returns>
        public object Process()
        {
            var result = ProcessFilters();

            var serializer = new XmlSerializer(_source.GetType());

            using (var rdr = result.CreateReader())
                return serializer.Deserialize(rdr);
        }

        /// <summary>
        /// Filters the object
        /// </summary>
        /// <typeparam name="T">The type of the object being filtered</typeparam>
        /// <returns>
        /// A filtered instance of <typeparamref name="T"/>. 
        /// If the filtered object cannot be cast as type <typeparamref name="T"/> then an exception will be thrown.
        /// </returns>
        public T Process<T>()
        {
            return (T)Process();
        }

        /// <summary>
        /// Filters the object and returns the result as a Json string
        /// </summary>
        /// <returns>A Json string of the filtered object</returns>
        public string ProcessAsJson()
        {
            var result = ProcessFilters();
            var namespaces = new List<XAttribute>();
            namespaces.AddRange(result.Root.Attributes());

            foreach (var ns in namespaces)
                ns.Remove();

            return JsonConvert.SerializeXNode(result.Root, Formatting.Indented, true);
        }

        /// <summary>
        /// Filters the object and returns the result as an xml string
        /// </summary>
        /// <returns>An xml string of the filtered object</returns>
        public string ProcessAsXml()
        {
            return ProcessFilters().ToString();
        }

        /// <summary>
        /// Filters the object and returns it as an <see cref="XDocument"/>
        /// </summary>
        /// <returns>An <see cref="XDocument"/> of the filtered object</returns>
        public XDocument ProcessAsXDocument()
        {
            return ProcessFilters();
        }

        private XDocument ProcessFilters()
        {
            var xmlDoc = GetObjectXml(_source);

            var childrenToDelete = new List<XElement>();

            foreach (var node in xmlDoc.Root.Elements())
                Process(null, node, childrenToDelete);

            foreach (var deletion in childrenToDelete)
                deletion.Remove();

            return xmlDoc;
        }

        private void Process(string parentName, XElement node, List<XElement> elementsToDelete)
        {
            var currentName = GetParentName(parentName, node);

            var matchResult = IsMatch(currentName);
            if (matchResult.HasValue && matchResult.Value)
                return; //We have a match

            //We may have a partial match
            //Walk the children and find more matches 
            //I <3 recursion
            if (node.HasElements && matchResult.HasValue)
            {
                var childrenToDelete = new List<XElement>();

                foreach (var child in node.Elements())
                {
                    Process(currentName, child, childrenToDelete);
                }

                foreach (var deletion in childrenToDelete)
                    deletion.Remove();

                if (node.IsEmpty)
                    elementsToDelete.Add(node);
            }
            else //We either have no match or an override. Mark the item for deletion.
                elementsToDelete.Add(node);
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
