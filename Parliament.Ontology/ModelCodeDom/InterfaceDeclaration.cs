namespace Parliament.Ontology.ModelCodeDom
{
    using Parliament.Rdf;
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
            this.CustomAttributes.Add(new ResourceAttributeDeclaration<ClassAttribute>(this.ontologyClass));
        }

        private void AddSuperClasses()
        {
            // TODO: This might be unneccessary (move to Thing base class instead).
            if (this.ontologyClass.IsTopClass)
            {
                this.BaseTypes.Add(typeof(IResource));
            }
            
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