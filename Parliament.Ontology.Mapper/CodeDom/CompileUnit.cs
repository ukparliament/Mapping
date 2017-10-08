namespace Parliament.Ontology.Mapper.CodeDom
{
    using Parliament.Ontology.Base;
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
            this.AddReferenceByMarkerType<IOntologyInstance>();
        }

        private void AddReferenceByMarkerType<T>()
        {
            var assemblyName = typeof(T).Assembly.ManifestModule.Name;
            this.ReferencedAssemblies.Add(assemblyName);
        }

        private void AddNamespace()
        {
            var ontologyNamespace = new Namespace(this.name, this.ontology);
            this.Namespaces.Add(ontologyNamespace);
        }
    }
}
