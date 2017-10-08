namespace Parliament.Ontology.Mapper.CodeDom
{
    using Parliament.Ontology.Base;
    using System.CodeDom;
    using System.Linq;
    using VDS.RDF.Ontology;

    internal class InterfaceProperty : CodeMemberProperty
    {
        private OntologyProperty ontologyProperty;

        internal InterfaceProperty(OntologyProperty ontologyProperty)
        {
            this.ontologyProperty = ontologyProperty;

            this.Initialize();
            this.AddType();
            this.AddPredicateAttribute();
            // TODO: Is this redundant? Could always get range uri from object property type interface declaration class attribute
            this.AddRangeAttribute();
        }

        private void Initialize()
        {
            this.HasGet = true;
            this.HasSet = true;
            this.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            this.Name = this.ontologyProperty.ToPascalCase();
        }

        private void AddPredicateAttribute()
        {
            this.CustomAttributes.Add(new ResourceAttributeDeclaration<PropertyAttribute>(this.ontologyProperty));
        }

        private void AddType()
        {
            this.Type = this.ontologyProperty.ToTypeReference();
        }

        private void AddRangeAttribute()
        {
            var rangeClass = this.ontologyProperty.Ranges.FirstOrDefault();

            if (rangeClass != null)
            {
                this.CustomAttributes.Add(new ResourceAttributeDeclaration<RangeAttribute>(rangeClass));
            }
        }
    }
}