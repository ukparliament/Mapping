namespace Parliament.Ontology.ModelCodeDom
{
    using Parliament.Serialization;
    using System.CodeDom;
    using System.Linq;
    using VDS.RDF.Ontology;

    public class PropertyWithBackingField : CodeTypeMemberCollection
    {
        private readonly string name;
        private readonly CodeTypeReference type;
        private readonly OntologyProperty ontologyProperty;

        public PropertyWithBackingField(OntologyProperty ontologyProperty)
        {
            this.name = ontologyProperty.ToPascalCase();
            this.type = ontologyProperty.ToTypeReference(false);
            this.ontologyProperty = ontologyProperty;

            this.AddField();
            this.AddProperty();
        }

        private void AddField()
        {
            var privateFiled = new CodeMemberField();
            privateFiled.Name = this.name.Lowercase();
            privateFiled.Attributes = MemberAttributes.Private;
            privateFiled.Type = this.type;

            this.Add(privateFiled);
        }

        private void AddProperty()
        {
            var fieldName = this.name.Lowercase();

            var property = new CodeMemberProperty();
            property.Name = this.name;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            property.Type = this.type;

            var fieldReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
            property.GetStatements.Add(new CodeMethodReturnStatement(fieldReference));
            property.SetStatements.Add(new CodeAssignStatement(fieldReference, new CodePropertySetValueReferenceExpression()));

            property.CustomAttributes.Add(new ResourceAttributeDeclaration<PropertyAttribute>(this.ontologyProperty));
            var rangeClass = this.ontologyProperty.Ranges.FirstOrDefault();

            if (rangeClass != null)
            {
                property.CustomAttributes.Add(new ResourceAttributeDeclaration<RangeAttribute>(rangeClass));
            }

            this.Add(property);
        }
    }
}