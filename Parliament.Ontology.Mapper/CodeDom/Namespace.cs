namespace Parliament.Ontology.Mapper.CodeDom
{
    using System.CodeDom;
    using VDS.RDF.Ontology;

    internal class Namespace : CodeNamespace
    {
        private OntologyGraph ontology;

        internal Namespace(string name, OntologyGraph ontology) : base(name)
        {
            this.ontology = ontology;

            this.AddImports();
            this.AddInterfaces();
            this.AddClasses();
        }

        private void AddImports()
        {
            var systemImport = new CodeNamespaceImport(nameof(System));
            this.Imports.Add(systemImport);
        }

        private void AddInterfaces()
        {
            // TODO: Do we need to implement extrernal classes (i.e. skos:Concept)? They probably don't have properties.
            foreach (var ontologyClass in this.ontology.AllClasses)
            {
                var ontologyInterface = new InterfaceDeclaration(ontologyClass);
                this.Types.Add(ontologyInterface);
            }
        }

        private void AddClasses()
        {
            // TODO: Do we need to implement extrernal classes (i.e. skos:Concept)? They probably don't have properties.
            foreach (var ontologyClass in this.ontology.AllClasses)
            {
                var ontologyClassTypeDeclaration = new ClassDeclaration(ontologyClass);
                this.Types.Add(ontologyClassTypeDeclaration);
            }
        }
    }
}