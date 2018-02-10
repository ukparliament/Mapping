namespace Parliament.Ontology.ModelCodeDom
{
    using Parliament.Rdf;
    using System;
    using System.CodeDom;
    using VDS.RDF.Ontology;

    internal class CompileUnit : CodeCompileUnit
    {
        private readonly OntologyGraph ontology;
        private readonly string name;

        internal CompileUnit(string name, OntologyGraph ontology)
        {
            this.ontology = ontology;
            this.name = name;

            this.AddReferences();
            this.AddNamespace();
        }

        private void AddReferences()
        {
            this.AddReferenceByMarkerType<Uri>();
            this.AddReferenceByMarkerType<IResource>();
        }

        private void AddReferenceByMarkerType<T>()
        {
            this.ReferencedAssemblies.Add(typeof(T).Assembly.Location);
        }

        private void AddNamespace()
        {
            var ontologyNamespace = new Namespace(this.name, this.ontology);
            this.Namespaces.Add(ontologyNamespace);
        }
    }
}
