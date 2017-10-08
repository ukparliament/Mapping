namespace Parliament.Ontology.Mapper
{
    using Microsoft.CSharp;
    using Parliament.Ontology.Mapper.CodeDom;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.IO;
    using VDS.RDF;
    using VDS.RDF.Ontology;

    public static class Generator
    {
        public static CompilerResults CompileAssembly(string ontologyFilePath, string namespaceName)
        {
            var dom = Generator.GenerateCodeDom(ontologyFilePath, namespaceName);
            var parameters = new CompilerParameters()
            {
                OutputAssembly = $"{namespaceName}.dll"
            };

            using (var provider = new CSharpCodeProvider())
            {
                return provider.CompileAssemblyFromDom(parameters, dom);
            }
        }

        public static string GenerateCode(string ontologyFilePath, string namespaceName)
        {
            var dom = Generator.GenerateCodeDom(ontologyFilePath, namespaceName);

            using (var provider = new CSharpCodeProvider())
            {
                using (var writer = new StringWriter())
                {
                    provider.GenerateCodeFromCompileUnit(dom, writer, null);

                    return writer.ToString();
                }
            }
        }

        private static CodeCompileUnit GenerateCodeDom(string ontologyFilePath, string namespaceName)
        {
            var ontology = new OntologyGraph();
            ontology.LoadFromFile(ontologyFilePath);

            return new CompileUnit(namespaceName, ontology);
        }
    }
}
