﻿namespace Parliament.Ontology.ModelCodeDom
{
    using Parliament.Rdf.Serialization;
    using System;
    using System.CodeDom;
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
            this.AddPropertiesFrom(this.ontologyClass, compileUnitOption);

            foreach (var superClass in this.ontologyClass.SuperClasses)
            {
                this.AddPropertiesFrom(superClass, compileUnitOption);
            }

            foreach (var subClass in this.ontologyClass.SubClasses)
            {
                this.AddPropertiesFrom(subClass, compileUnitOption);
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