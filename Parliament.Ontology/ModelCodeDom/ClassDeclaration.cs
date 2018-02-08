namespace Parliament.Ontology.ModelCodeDom
{
    using Parliament.Rdf;
    using System;
    using System.CodeDom;
    using System.Reflection;
    using VDS.RDF.Ontology;

    internal class ClassDeclaration : CodeTypeDeclaration
    {
        private OntologyClass ontologyClass;

        internal ClassDeclaration(OntologyClass ontologyClass)
        {
            this.ontologyClass = ontologyClass;

            this.Name = this.ontologyClass.ToPascalCase();
            this.BaseTypes.Add(this.ontologyClass.ToInterfaceName());
            this.TypeAttributes = TypeAttributes.Public | TypeAttributes.Class;

            this.AddIResourceProperties();
            this.AddProperties();
        }

        private void AddIResourceProperties()
        {
            CodeTypeReference uriType = new CodeTypeReference(typeof(Uri));

            string idName = nameof(IResource.Id);
            this.Members.AddRange(new PropertyWithBackingField(idName, uriType));

            string baseUriName = nameof(IResource.BaseUri);
            this.Members.AddRange(new PropertyWithBackingField(baseUriName, uriType));

            CodeMemberField localIdField = new CodeMemberField();
            localIdField.Name = nameof(IResource.LocalId).Lowercase();
            localIdField.Type = new CodeTypeReference(typeof(string));
            localIdField.Attributes = MemberAttributes.Private;
            this.Members.Add(localIdField);

            CodeMemberProperty localIdProperty = new CodeMemberProperty();
            localIdProperty.Name = nameof(IResource.LocalId);
            localIdProperty.Type = new CodeTypeReference(typeof(string));
            localIdProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            CodeBinaryOperatorExpression condition = new CodeBinaryOperatorExpression(
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression(baseUriName), CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)),
                CodeBinaryOperatorType.BooleanAnd,
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression(idName), CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null))
                );
            CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(new CodeMethodInvokeExpression(                    
                        new CodeVariableReferenceExpression(baseUriName), "MakeRelativeUri", new CodeExpression[] { new CodeVariableReferenceExpression(idName) }),
                    "ToString"
                ));
            localIdProperty.GetStatements.Add(
                new CodeConditionStatement(
                    condition,
                    new CodeStatement[] { returnStatement },
                    new CodeStatement[] { new CodeMethodReturnStatement(new CodePrimitiveExpression(null)) }
                ));
            this.Members.Add(localIdProperty);
        }

        private string localId;
        public string LocalId
        {
            get
            {
                return new Uri("").MakeRelativeUri(new Uri("")).ToString();
            }
        }

        private void AddProperties()
        {
            this.AddPropertiesFrom(this.ontologyClass);

            foreach (var superClass in this.ontologyClass.SuperClasses)
            {
                this.AddPropertiesFrom(superClass);
            }
        }

        private void AddPropertiesFrom(OntologyClass ontologyClass)
        {
            foreach (var ontologyProperty in ontologyClass.IsDomainOf)
            {
                var name = ontologyProperty.ToPascalCase();
                var type = ontologyProperty.ToTypeReference();

                this.Members.AddRange(new PropertyWithBackingField(name, type));
            }
        }
    }
}