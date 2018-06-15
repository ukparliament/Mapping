namespace Parliament.Ontology.ModelCodeDom
{
    using Parliament.Rdf.Serialization;
    using System;
    using System.CodeDom;
    using VDS.RDF.Ontology;

    internal class CompileUnit : CodeCompileUnit
    {
        private readonly OntologyGraph ontology;
        private readonly string name;

        internal CompileUnit(string name, OntologyGraph ontology,
            CompileUnitOption compileUnitOption, string modelOutputLocation = null)
        {
            this.ontology = ontology;
            this.name = name;
            this.AddReferences();
            this.AddNamespace(compileUnitOption);
        }

        private void AddReferences()
        {
            this.AddReferenceByMarkerType<Uri>();
            this.AddReferenceByMarkerType<BaseResource>();
        }

        private void AddReferenceByMarkerType<T>()
        {
            this.ReferencedAssemblies.Add(typeof(T).Assembly.Location);
        }

        private void AddNamespace(CompileUnitOption compileUnitOption)
        {
            var ontologyNamespace = new Namespace(this.name, this.ontology, compileUnitOption);
            this.Namespaces.Add(ontologyNamespace);
        }
    }
}
