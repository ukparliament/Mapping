namespace Parliament.Ontology.ModelCodeDom
{
    using System.CodeDom;

    public class PropertyWithBackingField : CodeTypeMemberCollection
    {
        private readonly string name;
        private readonly CodeTypeReference type;

        public PropertyWithBackingField(string name, CodeTypeReference type)
        {
            this.name = name;
            this.type = type;

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

            this.Add(property);
        }
    }
}