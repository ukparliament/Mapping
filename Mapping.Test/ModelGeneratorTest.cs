using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parliament.Ontology;
using Parliament.Ontology.ModelCodeDom;
using System.CodeDom.Compiler;

namespace Mapping.Test
{
    [TestClass]
    public class ModelGeneratorTest
    {
        [TestMethod]
        public void GenerateModelTest()
        {
            var result = ModelGenerator.CompileModelAssembly("Ontology.ttl", "Parliament.Model", System.IO.Path.GetTempPath());
            
            Assert.IsFalse(result.Errors.HasErrors);
        }

        // Generates a dll
        [TestMethod]
        public void GenerateModelImplementationTest()
        {
            var result = ModelGenerator.CompileModelImplementationAssembly("Ontology.ttl", "Parliament.Model", System.IO.Path.GetTempPath());

            Assert.IsFalse(result.Errors.HasErrors);
        }

        // Generates c# files
        [TestMethod]
        [DataRow("CSharp")]
        [DataRow("Ruby")]
        [DataRow("CPP")]
        [DataRow("JScript")]
        public void GenerateModelImplementationTextTest(string language)
        {
            CodeDomProvider codeDomProvider = null;
            if (language == "Ruby")
                codeDomProvider = new RubyCodeProvider();
            else
                codeDomProvider = CodeDomProvider.CreateProvider(language);
            object result= typeof(ModelGenerator)
                .GetMethod("GenerateModelImplementation")
                .MakeGenericMethod(codeDomProvider.GetType())
                .Invoke(null,new [] { "Ontology.ttl", "Parliament.Model" });

            Assert.IsInstanceOfType(result, typeof(string));
            Assert.IsNotNull(result);
        }

    }
}
