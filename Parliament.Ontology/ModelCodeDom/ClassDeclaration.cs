namespace Parliament.Ontology.ModelCodeDom
{
    using Parliament.Rdf.Serialization;
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using VDS.RDF.Ontology;

    internal class ClassDeclaration : CodeTypeDeclaration
    {
        private OntologyClass ontologyClass;

        internal ClassDeclaration(OntologyClass ontologyClass,
            CompileUnitOption compileUnitOption)
        {
            this.ontologyClass = ontologyClass;
            this.BaseTypes.Add(typeof(BaseResource));
            this.Name = this.ontologyClass.ToPascalCase();
            this.TypeAttributes = TypeAttributes.Public | TypeAttributes.Class;
            this.CustomAttributes.Add(new ResourceAttributeDeclaration<ClassAttribute>(this.ontologyClass));

            if ((ontologyClass.Label != null) && (ontologyClass.Label.Any()))
                foreach (VDS.RDF.ILiteralNode labelNode in ontologyClass.Label)
                    this.Comments.Add(new CodeCommentStatement(labelNode.Value));

            this.AddProperties(compileUnitOption);
        }

        private void AddProperties(CompileUnitOption compileUnitOption)
        {
            IEnumerable<OntologyClass> fullClassContext = null;
            if (this.ontologyClass.SuperClasses.Any())
            {
                fullClassContext = this.ontologyClass.SuperClasses
                    .Union(this.ontologyClass.SuperClasses
                        .SelectMany(sc => sc.SubClasses));                
            }
            else
            {
                fullClassContext = new OntologyClass[] { this.ontologyClass }
                    .Union(this.ontologyClass.SubClasses);
            }
            HashSet<string> classes = new HashSet<string>();
            foreach (var contextClass in fullClassContext)
            {
                if (classes.Contains(contextClass.ToPascalCase()) == false)
                {
                    this.AddPropertiesFrom(contextClass, compileUnitOption);
                    classes.Add(contextClass.ToPascalCase());
                }
            }
        }

        private void AddPropertiesFrom(OntologyClass ontologyClass, CompileUnitOption compileUnitOption)
        {
            foreach (var ontologyProperty in ontologyClass.IsDomainOf)
            {
                this.Members.AddRange(new PropertyWithBackingField(ontologyProperty));
            }
        }
    }
}