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

namespace Parliament.Ontology.Serializer.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SerializeSingularModel()
        {
            IBaseOntology t = createTerritoryModel();
            Serializer s = new Serializer();
            Graph g0 = s.Serialize(new IBaseOntology[] { t }, typeof(Parliament.Ontology.Code.Approval).Assembly);
            Assert.AreEqual(g0.Triples.Count, 18);
        }

        [TestMethod]
        public void SerializeSingularModelWithoutType()
        {
            IBaseOntology t = createTerritoryModel();
            Serializer s = new Serializer();
            Graph g0 = s.Serialize(new IBaseOntology[] { t }, typeof(Parliament.Ontology.Code.Approval).Assembly, SerializerOptions.ExcludeRdfType);
            Graph g1 = s.Serialize(new IBaseOntology[] { t }, typeof(Parliament.Ontology.Code.Approval).Assembly);
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
            IBaseOntology t = createTerritoryModel();
            Serializer s = new Serializer();
            Graph g0 = s.Serialize(new IBaseOntology[] { t }, typeof(Parliament.Ontology.Code.Approval).Assembly);
            IEnumerable<IBaseOntology> things = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);
            Graph g1 = s.Serialize(things, typeof(Parliament.Ontology.Code.Approval).Assembly);
            GraphDiffReport diff = g0.Difference(g1);
            Assert.AreEqual(diff.AddedTriples.Count(), 0);
            Assert.AreEqual(diff.RemovedTriples.Count(), 0);
        }

        [TestMethod]
        public void DeserializeMember()
        {
            string turtle = null;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Parliament.Ontology.Serializer.Test.Member.ttl"))
                using (StreamReader reader = new StreamReader(stream))
                    turtle=reader.ReadToEnd();
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IBaseOntology> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.House).Assembly);
            Assert.AreEqual(t.Count(), 2);
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
            IEnumerable<IBaseOntology> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);
            Assert.AreEqual(t.Count(), 2);
        }

        [TestMethod]
        public void SL1()
        {
            string turtle = @"
<http://territory.com/> a <http://id.ukpds.org/schema/Territory>.
<http://territory.com/> <http://id.ukpds.org/schema/containedByPlace> <http://abc.com/>.
<http://abc.com/> a <http://id.ukpds.org/schema/Place>.
<http://abc.com/> <http://id.ukpds.org/schema/containsPlace> <http://territory.com/>.
";
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IBaseOntology> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);

            var t1 = t.OfType<ITerritory>().Single();
            var p2 = t.OfType<IPlace>().Last();

            var p1cp = p2.ContainsPlace.Single();

            Assert.AreSame(p1cp, t1);
        }

        [TestMethod]
        public void SL2()
        {
            string turtle = @"
<http://territory.com/> a <http://id.ukpds.org/schema/Territory>.
<http://territory.com/> <http://id.ukpds.org/schema/containedByPlace> <http://abc.com/>.
<http://abc.com/> a <http://id.ukpds.org/schema/Person>.
<http://abc.com/> <http://id.ukpds.org/schema/containsPlace> <http://territory.com/>.
";
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IBaseOntology> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);

            var numberOfPlaces = t.OfType<IPlace>().Count();
            Assert.AreEqual(numberOfPlaces, 1);
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
            IEnumerable<IBaseOntology> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);

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
            string turtle = @"<http://territory.com/> a <http://id.ukpds.org/schema/Territory>.
<http://territory.com/> <http://id.ukpds.org/schema/containedByPlace> <http://abc.com/>.
<http://abc.com/> a <http://id.ukpds.org/schema/Place>.
<http://abc.com/> <http://id.ukpds.org/schema/containsPlace> <http://territory.com/>.";
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IBaseOntology> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);
            Graph g1 = s.Serialize(t, typeof(Parliament.Ontology.Code.Approval).Assembly);
            GraphDiffReport diff = g0.Difference(g1);
            Assert.AreEqual(diff.AddedTriples.Count(), 0);
            Assert.AreEqual(diff.RemovedTriples.Count(), 0);
        }

        [TestMethod]
        public void DeserializeSubjectsWithoutObjects()
        {
            string turtle = @"<http://territory.com/> a <http://id.ukpds.org/schema/Territory>.
<http://territory.com/> <http://id.ukpds.org/schema/containedByPlace> <http://abc.com/>.";
            Serializer s = new Serializer();
            Graph g0 = new Graph();
            g0.LoadFromString(turtle, new TurtleParser());
            IEnumerable<IBaseOntology> t = s.Deserialize(g0, typeof(Parliament.Ontology.Code.Approval).Assembly);
            Graph g1 = s.Serialize(t, typeof(Parliament.Ontology.Code.Approval).Assembly);
            GraphDiffReport diff = g0.Difference(g1);
            Triple removed = diff.RemovedTriples.SingleOrDefault();
            Assert.AreEqual(diff.RemovedTriples.Count(), 1);
            Assert.AreEqual(((IUriNode)removed.Object).Uri.ToString(), "http://abc.com/");
            Assert.AreEqual(((IUriNode)removed.Predicate).Uri.ToString(), "http://id.ukpds.org/schema/containedByPlace");
            Assert.AreEqual(diff.AddedTriples.Count(), 0);
        }

        private IBaseOntology createTerritoryModel()
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
            t.Latitude = 123;
            t.PlaceHasLocatedSignatureCount = new ILocatedSignatureCount[]
            {
                new LocatedSignatureCount()
                {
                    SubjectUri=new Uri("http://locatedsignatorecount1.com"),
                    SignatureCount = new int[] { 567 },
                    SignatureCountRetrievedAt=new DateTimeOffset[] { DateTimeOffset.UtcNow }
                },
                new LocatedSignatureCount()
                {
                    SubjectUri=new Uri("http://locatedsignatorecount2.com"),
                    SignatureCount = new int[] { 5678 }
                }
            };
            t.ContainsPlace = new IPlace[]
            {
                new Place()
                {
                    SubjectUri=new Uri("http://containsplace.com"),
                    PlaceHasLocatedSignatureCount = new ILocatedSignatureCount[]
                    {
                        new LocatedSignatureCount()
                        {
                            SubjectUri=new Uri("http://locatedsignatorecountcontainsplace.com"),
                            SignatureCount = new int[] { 890, 990 }
                        }
                    }
                }
            };
            return t;
        }
    }
}
