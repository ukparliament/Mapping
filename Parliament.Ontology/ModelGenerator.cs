﻿namespace Parliament.Ontology
{
    using Microsoft.CSharp;
    using Parliament.Ontology.ModelCodeDom;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.IO;
    using VDS.RDF;
    using VDS.RDF.Ontology;

    public static class ModelGenerator
    {
        public static CompilerResults CompileAssembly(string ontologyFilePath, string namespaceName, string outputLocation=null, bool generateInMemory = false)
        {
            var dom = ModelGenerator.GenerateCodeDom(ontologyFilePath, namespaceName);
            if (string.IsNullOrWhiteSpace(outputLocation))
                outputLocation = string.Empty;
            var parameters = new CompilerParameters()
            {
                OutputAssembly = $"{outputLocation}\\{namespaceName}.dll",
                GenerateInMemory = generateInMemory
            };

            using (var provider = new CSharpCodeProvider())
            {
                return provider.CompileAssemblyFromDom(parameters, dom);
            }
        }

        public static string GenerateCode(string ontologyFilePath, string namespaceName)
        {
            var dom = ModelGenerator.GenerateCodeDom(ontologyFilePath, namespaceName);

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
