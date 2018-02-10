using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parliament.Ontology;

namespace Mapping.Test
{
    [TestClass]
    public class ModelGeneratorTest
    {
        [TestMethod]
        public void GenerateModelTest()
        {
            var result = ModelGenerator.CompileAssembly("Ontology.ttl", "Parliament.Model", System.IO.Path.GetTempPath());
            Assert.IsFalse(result.Errors.HasErrors);
        }
    }
}
