using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parliament.Ontology;
using Parliament.Rdf.Serialization;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VDS.RDF;

namespace Mapping.Test
{
    [TestClass]
    public class SerializationTest
    {

        private readonly CompilerResults compilerResults;

        public SerializationTest()
        {
            compilerResults = ModelGenerator.CompileModelImplementationAssembly("Ontology.ttl", "Parliament.Model", System.IO.Path.GetTempPath());
        }

        [TestMethod]
        public void SerializeSimpleClasse()
        {
            string houseName = "test house";
            object house = createClassInstance("House");
            house.GetType().GetProperty("Id").SetValue(house, new Uri("http://example.org/123"));
            house.GetType().GetProperty("HouseName").SetValue(house, houseName);

            RdfSerializer serializer = new RdfSerializer();
            Graph result = serializer.Serialize(new BaseResource[] { house as BaseResource }, compilerResults.CompiledAssembly.GetTypes(), SerializerOptions.ExcludeRdfType);
            Assert.AreEqual(result.Triples.Count, 1);
            Assert.AreEqual(result.Triples.SingleOrDefault().Object.ToString(), houseName);
        }

        [TestMethod]
        public void SerializeComplexClasse()
        {
            object parliamentaryIncumbency = createClassInstance("ParliamentaryIncumbency");
            parliamentaryIncumbency.GetType().GetProperty("Id").SetValue(parliamentaryIncumbency, new Uri("http://example.org/123"));
            parliamentaryIncumbency.GetType().GetProperty("ParliamentaryIncumbencyStartDate").SetValue(parliamentaryIncumbency, (DateTimeOffset?)DateTimeOffset.UtcNow.Date);
            object member = createClassInstance("Member");
            parliamentaryIncumbency.GetType().GetProperty("ParliamentaryIncumbencyHasMember").SetValue(parliamentaryIncumbency, member);
            object contactPoint1 = createClassInstance("ContactPoint");
            object contactPoint2 = createClassInstance("ContactPoint");
            IEnumerable<object> values = new [] { contactPoint1, contactPoint2 }.Select(c=>c);
            MethodInfo castMethodValues = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(contactPoint1.GetType());
            object castValues = castMethodValues.Invoke(values, new object[] { values });
            parliamentaryIncumbency.GetType().GetProperty("ParliamentaryIncumbencyHasContactPoint").SetValue(parliamentaryIncumbency, castValues);
            member.GetType().GetProperty("Id").SetValue(member, new Uri("http://example.org/member"));
            member.GetType().GetProperty("PersonFamilyName").SetValue(member, "name");
            contactPoint1.GetType().GetProperty("Id").SetValue(contactPoint1, new Uri("http://example.org/contact1"));
            contactPoint1.GetType().GetProperty("FaxNumber").SetValue(contactPoint1, "fax1");
            contactPoint2.GetType().GetProperty("Id").SetValue(contactPoint2, new Uri("http://example.org/contact2"));
            contactPoint2.GetType().GetProperty("FaxNumber").SetValue(contactPoint2, "fax2");

            RdfSerializer serializer = new RdfSerializer();
            Graph result = serializer.Serialize(new BaseResource[] { parliamentaryIncumbency as BaseResource }, compilerResults.CompiledAssembly.GetTypes(), SerializerOptions.ExcludeRdfType);
            Assert.AreEqual(result.Triples.Count, 7);
        }

        private object createClassInstance(string className)
        {
            return Activator.CreateInstance(compilerResults.CompiledAssembly.GetType($"Parliament.Model.{className}"));
        }
    }
}
