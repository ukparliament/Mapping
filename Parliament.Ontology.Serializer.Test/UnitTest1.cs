namespace Parliament.Ontology.Serializer.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using VDS.RDF;
    using VDS.RDF.Parsing;

    [TestClass]
    public class UnitTest1
    {
        /*private static Assembly mappingAssembly = typeof(IPerson).Assembly;

        [TestMethod]
        public void SerializeSingularModel()
        {
            var territory = CreateTerritoryModel();
            var serializer = new Serializer();
            var graph = serializer.Serialize(new IOntologyInstance[] { territory }, mappingAssembly);
            Assert.AreEqual(graph.Triples.Count, 4);
        }

        [TestMethod]
        public void SerializeSingularModelWithoutType()
        {
            var territory = CreateTerritoryModel();
            var serializer = new Serializer();
            var graph = serializer.Serialize(new IOntologyInstance[] { territory }, mappingAssembly, SerializerOptions.ExcludeRdfType);
            Assert.AreEqual(graph.Triples.Count, 3);
        }

        [TestMethod]
        public void SerializeSingularModelAndDeserializeIt()
        {
            IOntologyInstance t = CreateTerritoryModel();
            Serializer s = new Serializer();
            Graph g0 = s.Serialize(new IOntologyInstance[] { t }, mappingAssembly);
            IEnumerable<IOntologyInstance> things = s.Deserialize(g0, mappingAssembly);
            Graph g1 = s.Serialize(things, mappingAssembly);
            GraphDiffReport diff = g0.Difference(g1);
            Assert.AreEqual(diff.AddedTriples.Count(), 0);
            Assert.AreEqual(diff.RemovedTriples.Count(), 0);
        }

        [TestMethod]
        public void DeserializeMember()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Parliament.Ontology.Serializer.Test.Member.ttl"))
            {
                using (var reader = new StreamReader(stream))
                {
                    using (var g = new Graph())
                    {
                        new TurtleParser().Load(g, reader);

                        var serializer = new Serializer();
                        var instances = serializer.Deserialize(g, mappingAssembly);

                        var nodes = g.Nodes.UriNodes()
                            .Where(x => new Uri("http://id.ukpds.org/").IsBaseOf(x.Uri))
                            .Where(x => !new Uri("http://id.ukpds.org/schema/").IsBaseOf(x.Uri));

                        Assert.AreEqual(instances.Count(), nodes.Count());
                    }
                }
            }
        }

        [TestMethod]
        public void Deserialize2Subjects()
        {
            string turtle = @"<http://territory.com/> a <http://id.ukpds.org/schema/Territory>.
<http://territory.com/> <http://id.ukpds.org/schema/containedByPlace> <http://abc.com/>.
<http://abc.com/> a <http://id.ukpds.org/schema/Place>.
<http://abc.com/> <http://id.ukpds.org/schema/containsPlace> <http://territory.com/>.";
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IOntologyInstance> t = s.Deserialize(g0, mappingAssembly);
            Assert.AreEqual(t.Count(), 2);
        }

        [TestMethod]
        public void SL1()
        {
            string turtle = @"
<http://example.com/s1> a <http://id.ukpds.org/schema/Person>.
<http://example.com/s1> <http://id.ukpds.org/schema/personHasFormalBodyMembership> <http://example.com/s2>.
<http://example.com/s2> a <http://id.ukpds.org/schema/FormalBodyMembership>.
<http://example.com/s2> <http://id.ukpds.org/schema/formalBodyMembershipHasPerson> <http://example.com/s1>.
";
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IOntologyInstance> t = s.Deserialize(g0, mappingAssembly);

            var t1 = t.OfType<IPerson>().Single();
            var p2 = t.OfType<IFormalBodyMembership>().Single();

            var p1cp = p2.FormalBodyMembershipHasPerson;

            Assert.AreSame(p1cp, t1);
        }

        [TestMethod]
        public void SL2()
        {
            string turtle = @"
<http://example.com/s1> a <http://id.ukpds.org/schema/Person>.
<http://example.com/s1> <http://id.ukpds.org/schema/personHasFormalBodyMembership> <http://example.com/s2>.
<http://example.com/s2> a <http://id.ukpds.org/schema/FormalBodyMembership>.
<http://example.com/s2> <http://id.ukpds.org/schema/formalBodyMembershipHasPerson> <http://example.com/s1>.
";
            var s = new Serializer();
            var g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            var t = s.Deserialize(g0, mappingAssembly);

            var numberOfMemberships = t.OfType<IFormalBodyMembership>().Count();
            Assert.AreEqual(numberOfMemberships, 1);

            var numberOfPeople = t.OfType<IPerson>().Count();
            Assert.AreEqual(numberOfPeople, 1);
            Assert.AreEqual(t.Count(), 2);
        }

        [TestMethod]
        public void SL_MultipleTypePredicates()
        {
            var turtle = @"
<http://territory.com/> a <http://id.ukpds.org/schema/Territory>.
<http://territory.com/> a <http://id.ukpds.org/schema/Person>.
";
            var s = new Serializer();
            var g = new Graph();
            g.LoadFromString(turtle, new TurtleParser());
            var instances = s.Deserialize(g, mappingAssembly);

            Assert.AreEqual(instances.OfType<IPerson>().Count(), 1);
            Assert.AreEqual(instances.OfType<IPlace>().Count(), 1);
        }

        [TestMethod]
        public void Deserialize2SubjectsAndSerializeThem()
        {
            string turtle = @"
<http://example.com/s1> a <http://id.ukpds.org/schema/Person>.
<http://example.com/s1> <http://id.ukpds.org/schema/personHasFormalBodyMembership> <http://example.com/s2>.
<http://example.com/s2> a <http://id.ukpds.org/schema/FormalBodyMembership>.
<http://example.com/s2> <http://id.ukpds.org/schema/formalBodyMembershipHasPerson> <http://example.com/s1>.
";
            var s = new Serializer();

            var originalGraph = new Graph();
            originalGraph.LoadFromString(turtle, new TurtleParser());

            var t = s.Deserialize(originalGraph, mappingAssembly);
            var newGraph = s.Serialize(t, mappingAssembly);
            var delta = originalGraph.Difference(newGraph);

            Assert.IsTrue(delta.AreEqual);
        }

        [TestMethod]
        public void DeserializeSubjectsWithoutObjects()
        {
            string turtle = @"<http://territory.com/> a <http://id.ukpds.org/schema/Territory>.
<http://territory.com/> <http://id.ukpds.org/schema/containedByPlace> <http://abc.com/>.";
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IOntologyInstance> t = s.Deserialize(g0, mappingAssembly);
            Graph g1 = s.Serialize(t, mappingAssembly);
            GraphDiffReport diff = g0.Difference(g1);
            Triple removed = diff.RemovedTriples.SingleOrDefault();
            Assert.AreEqual(diff.RemovedTriples.Count(), 1);
            Assert.AreEqual(((IUriNode)removed.Object).Uri.ToString(), "http://abc.com/");
            Assert.AreEqual(((IUriNode)removed.Predicate).Uri.ToString(), "http://id.ukpds.org/schema/containedByPlace");
            Assert.AreEqual(diff.AddedTriples.Count(), 0);
        }

        private IOntologyInstance CreateTerritoryModel()
        {
            return new Territory
            {
                SubjectUri = new Uri("http://territory.com"),
                TerritoryName = new[] {
                    "TerritoryName123",
                    "TerritoryName456" },
                TerritoryOfficialName = new[] {
                    "TerritoryOfficialName123" }
            };
        }*/
    }
}
