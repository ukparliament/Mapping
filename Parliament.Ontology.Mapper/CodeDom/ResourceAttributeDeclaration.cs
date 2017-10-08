namespace Parliament.Ontology.Mapper.CodeDom
{
    using Parliament.Ontology.Base;
    using System.CodeDom;
    using VDS.RDF;
    using VDS.RDF.Ontology;

    internal class ResourceAttributeDeclaration<T> : CodeAttributeDeclaration where T : ResourceAttribute
    {
        internal ResourceAttributeDeclaration(OntologyResource ontologyResource) : base(new CodeTypeReference(typeof(T)))
        {
            this.Arguments.Add(
                new CodeAttributeArgument(
                    new CodePrimitiveExpression(
                        ontologyResource.ToUri().AbsoluteUri)));
        }
    }
}
