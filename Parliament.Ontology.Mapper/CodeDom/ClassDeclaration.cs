namespace Parliament.Ontology.Mapper.CodeDom
{
    using Parliament.Ontology.Base;
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

            // TODO: Maybe add Thing base class.

            this.Name = this.ontologyClass.ToPascalCase();
            this.BaseTypes.Add(this.ontologyClass.ToInterfaceName());
            this.TypeAttributes = TypeAttributes.Public | TypeAttributes.Class;

            // TODO: Maybe unneccessary if Thing base.
            this.AddIdProperty();
            this.AddProperties();
        }

        // TODO: Maybe unneccessary if Thing base.
        private void AddIdProperty()
        {
            var name = nameof(IOntologyInstance.SubjectUri);
            var type = new CodeTypeReference(typeof(Uri));

            this.Members.AddRange(new PropertyWithBackingField(name, type));
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