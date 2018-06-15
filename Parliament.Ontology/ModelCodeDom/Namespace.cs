namespace Parliament.Ontology.ModelCodeDom
{
    using Parliament.Rdf.Serialization;
    using System.CodeDom;
    using VDS.RDF.Ontology;

    internal class Namespace : CodeNamespace
    {
        private OntologyGraph ontology;

        internal Namespace(string name, OntologyGraph ontology,
            CompileUnitOption compileUnitOption) : base(name)
        {
            this.ontology = ontology;

            if (compileUnitOption == CompileUnitOption.InterfaceOnly)
                this.AddInterfaces();
            else
                if (compileUnitOption == CompileUnitOption.ModelImplementation)
                    this.AddClasses(compileUnitOption);
        }

        private void AddInterfaces()
        {
            foreach (var ontologyClass in this.ontology.AllClasses)
            {
                var ontologyInterface = new InterfaceDeclaration(ontologyClass);
                this.Types.Add(ontologyInterface);
            }
        }

        private void AddClasses(CompileUnitOption compileUnitOption)
        {
            foreach (var ontologyClass in this.ontology.AllClasses)
            {
                var ontologyClassTypeDeclaration = new ClassDeclaration(ontologyClass, compileUnitOption);
                this.Types.Add(ontologyClassTypeDeclaration);
            }
        }
    }
}