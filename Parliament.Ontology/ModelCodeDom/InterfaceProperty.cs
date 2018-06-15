namespace Parliament.Ontology.ModelCodeDom
{
    using System.CodeDom;
    using VDS.RDF.Ontology;

    internal class InterfaceProperty : CodeMemberProperty
    {
        private OntologyProperty ontologyProperty;

        internal InterfaceProperty(OntologyProperty ontologyProperty)
        {
            this.ontologyProperty = ontologyProperty;

            this.Initialize();
            this.AddType();
        }

        private void Initialize()
        {
            this.HasGet = true;
            this.HasSet = true;
            this.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            this.Name = this.ontologyProperty.ToPascalCase();
        }

        private void AddType()
        {
            this.Type = this.ontologyProperty.ToTypeReference(true);
        }

    }
}