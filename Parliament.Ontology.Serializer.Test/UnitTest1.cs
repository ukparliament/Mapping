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
        [TestMethod]
        public void SerializeSingularModel()
        {
            IOntologyInstance t = createTerritoryModel();
            Serializer s = new Serializer();
            Graph g0 = s.Serialize(new IOntologyInstance[] { t }, typeof(Parliament.Ontology.Code.Approval).Assembly);
            Assert.AreEqual(g0.Triples.Count, 18);
        }

        [TestMethod]
        public void SerializeSingularModelWithoutType()
        {
            IOntologyInstance t = createTerritoryModel();
            Serializer s = new Serializer();
            Graph g0 = s.Serialize(new IOntologyInstance[] { t }, typeof(Parliament.Ontology.Code.Approval).Assembly, SerializerOptions.ExcludeRdfType);
            Graph g1 = s.Serialize(new IOntologyInstance[] { t }, typeof(Parliament.Ontology.Code.Approval).Assembly);
            GraphDiffReport diff = g0.Difference(g1);
            Uri predicate = diff.AddedTriples
                .Select(tr => tr.Predicate as IUriNode)
                .Distinct()
                .SingleOrDefault()
                .Uri;

            Assert.AreEqual(g0.Triples.Count, 13);
            Assert.AreEqual(g1.Triples.Count, 18);
            Assert.AreEqual(diff.AddedTriples.Count(), 5);
            Assert.AreEqual(predicate.ToString(), RdfSpecsHelper.RdfType);
        }

        [TestMethod]
        public void SerializeSingularModelAndDeserializeIt()
        {
            IOntologyInstance t = createTerritoryModel();
            Serializer s = new Serializer();
            Graph g0 = s.Serialize(new IOntologyInstance[] { t }, typeof(Parliament.Ontology.Code.Approval).Assembly);
            IEnumerable<IOntologyInstance> things = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);
            Graph g1 = s.Serialize(things, typeof(Parliament.Ontology.Code.Approval).Assembly);
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
                        var instances = serializer.Deserialize(g, typeof(IPerson).Assembly);

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
            IEnumerable<IOntologyInstance> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);
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
            IEnumerable<IOntologyInstance> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);

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
            var t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);

            var numberOfMemberships = t.OfType<IFormalBodyMembership>().Count();
            Assert.AreEqual(numberOfMemberships, 1);

            var numberOfPeople = t.OfType<IPerson>().Count();
            Assert.AreEqual(numberOfPeople, 1);
            Assert.AreEqual(t.Count(), 2);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SL_MultipleTypePredicates()
        {
            string turtle = @"
<http://territory.com/> a <http://id.ukpds.org/schema/Territory>.
<http://territory.com/> a <http://id.ukpds.org/schema/Person>.
";
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IOntologyInstance> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);

            var instance = t.Single();
            Assert.IsInstanceOfType(instance, typeof(IPerson));
            Assert.IsInstanceOfType(instance, typeof(IPlace));

            // Should facilitate:
            //var person = instance as IPerson;
            //var firstName = person.PersonGivenName;
            //var lastName = person.PersonFamilyName;

            //var place = instance as IPlace;
            //var latitude = place.Latitude;
            //var longitude = place.Longitude;
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

            var t = s.Deserialize(originalGraph, typeof(Parliament.Ontology.Code.Approval).Assembly);
            var newGraph = s.Serialize(t, typeof(Parliament.Ontology.Code.Approval).Assembly);
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
            IEnumerable<IOntologyInstance> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);
            Graph g1 = s.Serialize(t, typeof(Parliament.Ontology.Code.Approval).Assembly);
            GraphDiffReport diff = g0.Difference(g1);
            Triple removed = diff.RemovedTriples.SingleOrDefault();
            Assert.AreEqual(diff.RemovedTriples.Count(), 1);
            Assert.AreEqual(((IUriNode)removed.Object).Uri.ToString(), "http://abc.com/");
            Assert.AreEqual(((IUriNode)removed.Predicate).Uri.ToString(), "http://id.ukpds.org/schema/containedByPlace");
            Assert.AreEqual(diff.AddedTriples.Count(), 0);
        }

        private IOntologyInstance createTerritoryModel()
        {
            /*Territory t2 = new Territory();
            t2.SubjectUri = new Uri("http://territory.com");
            t2.TerritoryOfficialName = new string[] { "TerritoryOfficialName123" };
            return t2;
            Member m = new Member();
            m.SubjectUri = new Uri("http://example.com");
            m.PersonFamilyName = "a";
            return m;*/
            //HouseSeat
            //Gender
            //Party
            //(p.PropertyType.GenericTypeArguments.Any()) && (p.PropertyType.GenericTypeArguments.SingleOrDefault().BaseType == null)

            Territory t = new Territory();
            t.SubjectUri = new Uri("http://territory.com");
            t.TerritoryName = new string[] { "TerritoryName123", "TerritoryName456" };
            t.TerritoryOfficialName = new string[] { "TerritoryOfficialName123" };
            return t;
        }
    }
}
