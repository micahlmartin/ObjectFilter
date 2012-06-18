using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Xml.XPath;

namespace ObjectFilter.Tests
{
    [TestFixture]
    public class ObjectFilterTest
    {
        private TestObject TestData;

        [TestFixtureSetUp]
        public void Setup()
        {
            TestData = new TestObject
            {
                Property1 = "1",
                Property2 = "2",
                Property3 = 3,
                SubObject = new TestObject
                {
                    Property1 = "S1",
                    Property2 = "S2",
                    Property3 = 3,
                    SubObject = new TestObject
                    {
                        Property1 = "SS1",
                        Property2 = "SS2",
                        Property3 = 3
                    }
                }
            };
        }

        [Test]
        public void Wildcard()
        {
            var filters = new[] { "*" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.Process<TestObject>();
            Assert.AreEqual(result.Property1, "1");
            Assert.AreEqual(result.SubObject.Property2, "S2");
            Assert.AreEqual(result.SubObject.SubObject.Property3, 3);
            Assert.IsNull(result.SubObject.SubObject.SubObject);
        }

        [Test]
        public void MultipleProperties()
        {
            var filters = new[] { "Property1", "SubObject/Property2", "SubObject/SubObject/Property3" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.Process<TestObject>();
            Assert.AreEqual("1", result.Property1);
            Assert.IsNull(result.Property2);
            Assert.AreEqual(0, result.Property3);
            Assert.AreEqual("S2", result.SubObject.Property2);
            Assert.IsNull(result.SubObject.Property1);
            Assert.AreEqual(0, result.SubObject.Property3);
            Assert.AreEqual(3, result.SubObject.SubObject.Property3);
            Assert.IsNull(result.SubObject.SubObject.Property1);
            Assert.IsNull(result.SubObject.SubObject.Property2);
            Assert.IsNull(result.SubObject.SubObject.SubObject);
        }

        [Test]
        public void MultipleSubPropertiesWithSubSelect()
        {
            var filters = new[] { "*", "SubObject(Property1,Property2)" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.Process<TestObject>();
            Assert.AreEqual("1", result.Property1);
            Assert.AreEqual("2", result.Property2);
            Assert.AreEqual(3, result.Property3);
            Assert.AreEqual("S1", result.SubObject.Property1);
            Assert.AreEqual("S2", result.SubObject.Property2);
            Assert.AreEqual(0, result.SubObject.Property3);
            Assert.IsNull(result.SubObject.SubObject);
        }

        [Test]
        public void MultipleSubProperties()
        {
            var filters = new[] { "*", "SubObject/Property1", "SubObject/SubObject/Property3" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.Process<TestObject>();
            Assert.AreEqual("1", result.Property1);
            Assert.AreEqual("2", result.Property2);
            Assert.AreEqual(3, result.Property3);
            Assert.AreEqual("S1", result.SubObject.Property1);
            Assert.IsNull(result.SubObject.Property2);
            Assert.AreEqual(0, result.SubObject.Property3);
            Assert.IsNull(result.SubObject.SubObject.Property1);
            Assert.IsNull(result.SubObject.SubObject.Property2);
            Assert.AreEqual(3, result.SubObject.SubObject.Property3);
        }

        [Test]
        public void NestedSubProperties()
        {
            var filters = new[] { "Property1", "SubObject/SubObject(Property1,Property2)" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.Process<TestObject>();
            Assert.AreEqual("1", result.Property1);
            Assert.IsNull(result.Property2);
            Assert.AreEqual(0, result.Property3);
            Assert.IsNull(result.SubObject.Property1);
            Assert.IsNull(result.SubObject.Property2);
            Assert.AreEqual(0, result.SubObject.Property3);
            Assert.AreEqual("SS1", result.SubObject.SubObject.Property1);
            Assert.AreEqual("SS2", result.SubObject.SubObject.Property2);
            Assert.AreEqual(0, result.SubObject.SubObject.Property3);

        }

        [Test]
        public void SingleProperty()
        {
            var filters = new[] { "Property1" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.Process<TestObject>();
            Assert.AreEqual("1", result.Property1);
            Assert.IsNull(result.Property2);
            Assert.AreEqual(0, result.Property3);
            Assert.IsNull(result.SubObject);
        }

        [Test]
        public void SingleSubProperty()
        {
            var filters = new[] { "SubObject/Property1" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.Process<TestObject>();
            Assert.IsNull(result.Property1);
            Assert.IsNull(result.Property2);
            Assert.AreEqual(0, result.Property3);
            Assert.AreEqual("S1", result.SubObject.Property1);
            Assert.IsNull(result.SubObject.Property2);
            Assert.AreEqual(0, result.SubObject.Property3);
            Assert.IsNull(result.SubObject.SubObject);
        }

        [Test]
        public void SubElementsWithWildcard()
        {
            var filters = new[] { "SubObject/*", "SubObject/SubObject/Property1" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.Process<TestObject>();
            Assert.IsNull(result.Property1);
            Assert.IsNull(result.Property2);
            Assert.AreEqual(0, result.Property3);
            Assert.AreEqual("S1", result.SubObject.Property1);
            Assert.AreEqual("S2", result.SubObject.Property2);
            Assert.AreEqual(3, result.SubObject.Property3);
            Assert.AreEqual("SS1", result.SubObject.SubObject.Property1);
            Assert.IsNull(result.SubObject.SubObject.Property2);
            Assert.AreEqual(0, result.SubObject.SubObject.Property3);
        }

        [Test]
        public void Serialization()
        {
            var filters = new[] { "*" };
            var filter = new FilterProcessor(TestData, filters);

            var result = (TestObject)filter.Process();
            Assert.AreEqual(result.Property1, "1");
            Assert.AreEqual(result.SubObject.Property2, "S2");
            Assert.AreEqual(3, result.SubObject.SubObject.Property3);
            Assert.IsNull(result.SubObject.SubObject.SubObject);
        }

        [Test]
        public void ProcessAsJson()
        {
            var filters = new[] { "Property1", "Property3", "SubObject/Property2" };
            var filter = new FilterProcessor(TestData, filters);

            var json = filter.ProcessAsJson();
            var result = JsonConvert.DeserializeObject<TestObject>(json);

            Assert.AreEqual("1", result.Property1);
            Assert.IsNull(result.Property2);
            Assert.AreEqual(3, result.Property3);
            Assert.IsNull(result.SubObject.Property1);
            Assert.AreEqual("S2", result.SubObject.Property2);
            Assert.AreEqual(0, result.SubObject.Property3);
            Assert.IsNull(result.SubObject.SubObject);
        }

        [Test]
        public void ProcessAsXml()
        {
            var filters = new[] { "Property1", "Property3", "SubObject/Property2" };
            var filter = new FilterProcessor(TestData, filters);

            var xml = filter.ProcessAsXml();
            var result = XDocument.Parse(xml);

            Assert.AreEqual("1", result.Root.XPathSelectElement("Property1").Value);
            Assert.IsNull(result.Root.XPathSelectElement("Property2"));
            Assert.AreEqual("3", result.Root.XPathSelectElement("Property3").Value);
            Assert.IsNull(result.Root.XPathSelectElement("SubObject/Property1"));
            Assert.AreEqual("S2", result.Root.XPathSelectElement("SubObject/Property2").Value);
            Assert.IsNull(result.Root.XPathSelectElement("SubObject/Property3"));
            Assert.IsNull(result.Root.XPathSelectElement("SubObject/SubObject"));
        }

        [Test]
        public void ProcessAsXDocument()
        {
            var filters = new[] { "Property1", "Property3", "SubObject/Property2" };
            var filter = new FilterProcessor(TestData, filters);

            var result = filter.ProcessAsXDocument();

            Assert.AreEqual("1", result.Root.XPathSelectElement("Property1").Value);
            Assert.IsNull(result.Root.XPathSelectElement("Property2"));
            Assert.AreEqual("3", result.Root.XPathSelectElement("Property3").Value);
            Assert.IsNull(result.Root.XPathSelectElement("SubObject/Property1"));
            Assert.AreEqual("S2", result.Root.XPathSelectElement("SubObject/Property2").Value);
            Assert.IsNull(result.Root.XPathSelectElement("SubObject/Property3"));
            Assert.IsNull(result.Root.XPathSelectElement("SubObject/SubObject"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullFiltersSpecifiedThrowsException()
        {
            var filter = new FilterProcessor(TestData, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void EmptyFiltersThrowsException()
        {
            var filter = new FilterProcessor(TestData, new string[0]);
        }

        [Test]
        public void NullOrEmptyFilterIsSkipped()
        {
            var filter = new FilterProcessor(TestData, new string[] { "*", null, "" });

            var result = filter.Process<TestObject>();
            Assert.AreEqual(result.Property1, "1");
            Assert.AreEqual(result.SubObject.Property2, "S2");
            Assert.AreEqual(result.SubObject.SubObject.Property3, 3);
            Assert.IsNull(result.SubObject.SubObject.SubObject);
        }
    }

    public class TestObject
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
        public int Property3 { get; set; }
        public TestObject SubObject { get; set; }
    }
}
