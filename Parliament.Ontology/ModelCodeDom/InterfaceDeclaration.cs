namespace Parliament.Ontology.ModelCodeDom
{
    using System.CodeDom;
    using System.Reflection;
    using VDS.RDF.Ontology;

    internal class InterfaceDeclaration : CodeTypeDeclaration
    {
        private OntologyClass ontologyClass;

        internal InterfaceDeclaration(OntologyClass ontologyClass)
        {
            this.ontologyClass = ontologyClass;

            this.Initialize();
            this.AddSuperClasses();
            this.AddProperties();
        }

        private void Initialize()
        {
            this.TypeAttributes = TypeAttributes.Public | TypeAttributes.Interface;
            this.Name = this.ontologyClass.ToInterfaceName();
        }

        private void AddSuperClasses()
        {
            foreach (var superClass in this.ontologyClass.SuperClasses)
            {
                var superClassName = superClass.ToInterfaceName();
                this.BaseTypes.Add(superClassName);
            }
        }

        private void AddProperties()
        {
            foreach (var ontologyProperty in this.ontologyClass.IsDomainOf)
            {
                var interfaceProperty = new InterfaceProperty(ontologyProperty);
                this.Members.Add(interfaceProperty);
            }
        }
    }
}