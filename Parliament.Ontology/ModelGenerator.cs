namespace Parliament.Ontology
{
    using Microsoft.CSharp;
    using Parliament.Ontology.ModelCodeDom;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using VDS.RDF;
    using VDS.RDF.Ontology;

    public static class ModelGenerator
    {
        public static CompilerResults CompileAssembly(string ontologyFilePath, string namespaceName, string outputLocation = null, bool generateInMemory = false)
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
            IUriNode ontologyNode = ontology.GetTriplesWithObject(ontology.CreateUriNode("owl:Ontology"))
                .SingleOrDefault()
                .Subject as IUriNode;
            Triple[] externalTriples = ontology.AllClasses
                .Where(c => c.IsDefinedBy.Any(cn => cn.Equals(ontologyNode)) == false)
                .SelectMany(c => c.TriplesWithSubject)
                .Union(ontology.AllClasses
                    .SelectMany(c => c.SuperClasses)
                    .Where(sc => sc.IsDefinedBy.Any(scn => scn.Equals(ontologyNode)) == false)
                    .SelectMany(sc => sc.TriplesWithObject))
                .ToArray();
            foreach (Triple triple in externalTriples)
                ontology.Retract(triple);

            return new CompileUnit(namespaceName, ontology);
        }
    }
}
