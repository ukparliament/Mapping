namespace Parliament.Ontology
{
    using Microsoft.CSharp;
    using Parliament.Ontology.ModelCodeDom;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Linq;
    using VDS.RDF;
    using VDS.RDF.Ontology;

    public static class ModelGenerator
    {
        public static CompilerResults CompileModelAssembly(string ontologyFilePath, string namespaceName, string outputLocation = null, bool generateInMemory = false)
        {
            var dom = ModelGenerator.GenerateModelInterfaceDom(ontologyFilePath, namespaceName);
            return GenerateAssembly(namespaceName, outputLocation, generateInMemory, dom);
        }

        public static CompilerResults CompileModelImplementationAssembly(string ontologyFilePath, string namespaceName, string outputLocation = null, bool generateInMemory = false)
        {
            var dom = ModelGenerator.GenerateModelImplementationDom(ontologyFilePath, namespaceName);

            return GenerateAssembly(namespaceName, outputLocation, generateInMemory, dom);
        }

        public static string GenerateModelImplementation<T>(string ontologyFilePath, string namespaceName) where T : CodeDomProvider, new()
        {
            var dom = ModelGenerator.GenerateModelImplementationDom(ontologyFilePath, namespaceName);
            using (CodeDomProvider codeDomProvider = new T())
            {
                using (StringWriter writer = new StringWriter())
                {
                    codeDomProvider.GenerateCodeFromCompileUnit(dom, writer, null);
                    return writer.ToString();
                }
            }
        }

        private static CompilerResults GenerateAssembly(string namespaceName, string outputLocation, bool generateInMemory, CodeCompileUnit dom)
        {
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

        private static CodeCompileUnit GenerateModelInterfaceDom(string ontologyFilePath, string namespaceName)
        {
            OntologyGraph ontology = GenerateOntologyGraph(ontologyFilePath);

            return new CompileUnit(namespaceName, ontology, CompileUnitOption.InterfaceOnly);
        }

        private static CodeCompileUnit GenerateModelImplementationDom(string ontologyFilePath, string namespaceName)
        {
            OntologyGraph ontology = GenerateOntologyGraph(ontologyFilePath);

            return new CompileUnit(namespaceName, ontology, CompileUnitOption.ModelImplementation);
        }

        private static OntologyGraph GenerateOntologyGraph(string ontologyFilePath)
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
            return ontology;
        }
    }
}
