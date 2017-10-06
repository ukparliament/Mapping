using Parliament.Ontology.Base;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Parliament.Ontology.Mapper
{
    public class MapperGenerator
    {
        public string OutputCode(string ontologyFilePath, string namespaceName)
        {
            string language = ConfigurationManager.AppSettings["OntologyFileLanguage"];
            if (string.IsNullOrWhiteSpace(language))
                language = CodeDomProvider.GetAllCompilerInfo()
                .FirstOrDefault()
                .GetLanguages()
                .FirstOrDefault();
            CodeCompileUnit codeCompileUnit = GenerateCodeDom(ontologyFilePath, namespaceName);
            CodeDomProvider provider = CodeDomProvider.CreateProvider(language);
            StringBuilder sb = new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
                provider.GenerateCodeFromCompileUnit(codeCompileUnit, writer, new CodeGeneratorOptions() { BlankLinesBetweenMembers = false });
            return sb.ToString();
        }

        public CodeCompileUnit GenerateCodeDom(string ontologyFilePath, string namespaceName)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace namespaceCode = new CodeNamespace(namespaceName);

            namespaceCode.Imports.Add(new CodeNamespaceImport(nameof(System)));
            compileUnit.ReferencedAssemblies.Add("System.dll");
            compileUnit.ReferencedAssemblies.Add(typeof(IBaseOntology).Assembly.ManifestModule.Name);
            compileUnit.Namespaces.Add(namespaceCode);

            Graph graph = new Graph();
            graph.LoadFromFile(ontologyFilePath);

            IEnumerable<IUriNode> classNodes = graph.GetTriplesWithPredicateObject(graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), graph.CreateUriNode("owl:Class"))
                .Union(graph.GetTriplesWithPredicateObject(graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), graph.CreateUriNode("rdfs:subClassOf")))
                .Select(t => (IUriNode)t.Subject)
                .Distinct();

            foreach (IUriNode classNode in classNodes)
            {
                CodeTypeDeclaration interfaceCode = generateInterface(graph, classNode);
                namespaceCode.Types.Add(interfaceCode);
            }

            /*IUriNode[] superClassNodes = graph.GetTriplesWithPredicateObject(graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), graph.CreateUriNode("owl:Class"))
                .Where(t => graph.GetTriplesWithPredicateObject(graph.CreateUriNode("rdfs:subClassOf"), t.Subject).Any() &&
                    graph.GetTriplesWithSubjectPredicate(t.Subject, graph.CreateUriNode("rdfs:subClassOf")).Any() == false)
                .Select(t => (IUriNode)t.Subject)
                .Distinct()
                .ToArray();

            foreach (IUriNode classNode in superClassNodes)
            {
                CodeTypeDeclaration interfaceCode = generateJoinedInterface(graph, classNode);
                namespaceCode.Types.Add(interfaceCode);
            }*/

            CodeTypeMember[] idProperty = generateIdProperty();

            foreach (IUriNode classNode in classNodes)
            {
                string className = classNode.Uri.GetNameFromUriNode(graph.NamespaceMap);
                CodeTypeDeclaration classCode = new CodeTypeDeclaration(className);
                classCode.TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;
                foreach (CodeTypeDeclaration type in namespaceCode.Types)
                {
                    if (type.Name == $"I{className}")
                    {
                        classCode.BaseTypes.Add(type.Name);
                        classCode.Members.AddRange(idProperty);
                        classCode.Members.AddRange(copyMembersFromInterface(type.Members).ToArray());
                        foreach (CodeTypeReference baseTypeReference in type.BaseTypes)
                        {
                            foreach (CodeTypeDeclaration baseType in namespaceCode.Types)
                            {
                                if (baseType.Name == baseTypeReference.BaseType)
                                {
                                    classCode.Members.AddRange(copyMembersFromInterface(baseType.Members).ToArray());
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                namespaceCode.Types.Add(classCode);
            }

            return compileUnit;
        }

        private CodeTypeMember[] generateIdProperty()
        {
            string idFieldName = typeof(IBaseOntology).GetProperties().SingleOrDefault().Name;
            CodeMemberField privateFiled = new CodeMemberField();
            privateFiled.Name = $"{idFieldName[0].ToString().ToLower()}{idFieldName.Substring(1)}";
            privateFiled.Attributes = MemberAttributes.Private;
            privateFiled.Type = new CodeTypeReference(typeof(Uri));

            CodeMemberProperty property = new CodeMemberProperty();
            property.Name = idFieldName;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            property.Type = new CodeTypeReference(typeof(Uri));
            property.GetStatements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(privateFiled.Name)));
            property.SetStatements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression(privateFiled.Name), new CodePropertySetValueReferenceExpression()));

            return new CodeTypeMember[] { privateFiled, property };
        }

        private CodeTypeDeclaration generateInterface(Graph graph, IUriNode classNode)
        {
            CodeTypeDeclaration interfaceCode = new CodeTypeDeclaration($"I{classNode.Uri.GetNameFromUriNode(graph.NamespaceMap)}");
            interfaceCode.TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Interface;
            CodeAttributeDeclaration interfaceTypeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(UriTypeAttribute)));
            interfaceTypeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(classNode.Uri.ToString())));
            interfaceCode.CustomAttributes.Add(interfaceTypeAttribute);
            interfaceCode.BaseTypes.Add(typeof(IBaseOntology));

            IEnumerable<IUriNode> ancestorNodes = getAllClassAncestors(graph, classNode)
                .Distinct();
            foreach (IUriNode ancestorNode in ancestorNodes)
            {
                string ancestorName = $"I{ancestorNode.Uri.GetNameFromUriNode(graph.NamespaceMap)}";
                interfaceCode.BaseTypes.Add(ancestorName);
            }
            CodeTypeMember[] properties = giveMeProperties(graph, classNode).ToArray();
            interfaceCode.Members.AddRange(properties);
            return interfaceCode;
        }

        private CodeTypeDeclaration generateJoinedInterface(Graph graph, IUriNode superClassNode)
        {
            CodeTypeDeclaration interfaceCode = new CodeTypeDeclaration($"I{superClassNode.Uri.GetNameFromUriNode(graph.NamespaceMap)}Joined");
            interfaceCode.TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Interface;
            CodeAttributeDeclaration interfaceTypeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(UriTypeAttribute)));
            interfaceTypeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(superClassNode.Uri.ToString())));
            interfaceCode.CustomAttributes.Add(interfaceTypeAttribute);
            interfaceCode.BaseTypes.Add(typeof(IBaseOntology));

            IEnumerable<IUriNode> offsprings = getClassOffsprings(graph, superClassNode)
                .Distinct();
            foreach (IUriNode subsetNode in offsprings)
            {
                string ancestorName = $"I{subsetNode.Uri.GetNameFromUriNode(graph.NamespaceMap)}";
                interfaceCode.BaseTypes.Add(ancestorName);
            }
            return interfaceCode;
        }

        private IEnumerable<IUriNode> getAllClassAncestors(IGraph graph, IUriNode classNode)
        {
            IEnumerable<IUriNode> ancestors = graph.GetTriplesWithSubjectPredicate(classNode, graph.CreateUriNode("rdfs:subClassOf"))
                .Where(t => t.Object.NodeType == NodeType.Uri)
                .Select(t => (IUriNode)t.Object)
                .Distinct();
            foreach (IUriNode ancestor in ancestors)
            {
                ancestors = ancestors.Concat(getAllClassAncestors(graph, ancestor));
            }
            return ancestors;
        }

        private IEnumerable<IUriNode> getClassOffsprings(IGraph graph, IUriNode classNode)
        {
            IEnumerable<IUriNode> offsprings = graph.GetTriplesWithPredicateObject(graph.CreateUriNode("rdfs:subClassOf"), classNode)
                .Where(t => t.Object.NodeType == NodeType.Uri)
                .Select(t => (IUriNode)t.Subject)
                .Distinct();
            if (offsprings.Any() == false)
                yield return classNode;
            else
                foreach (IUriNode offspring in offsprings)
                {
                    IEnumerable<IUriNode> nodes = getClassOffsprings(graph, offspring);
                    foreach (IUriNode node in nodes)
                        yield return node;
                }
        }

        private IEnumerable<CodeTypeMember> giveMeProperties(Graph graph, IUriNode classNode)
        {
            Uri stringXsdUri = new Uri("http://www.w3.org/2001/XMLSchema#string");
            IUriNode domainNode = graph.CreateUriNode("rdfs:domain");
            IUriNode rangeNode = graph.CreateUriNode("rdfs:range");            

            IEnumerable<IUriNode> propertyNodes = graph.GetTriplesWithPredicateObject(domainNode, classNode)
                    .Select(t => (IUriNode)t.Subject)
                    .Distinct();
            foreach (IUriNode propertyNode in propertyNodes)
            {
                string name = propertyNode.Uri.GetNameFromUriNode(graph.NamespaceMap);
                Triple functionalTriple = new Triple(propertyNode, graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), graph.CreateUriNode("owl:FunctionalProperty"));
                bool isFunctional = graph.ContainsTriple(functionalTriple);
                Uri rangeUri = graph.GetTriplesWithSubjectPredicate(propertyNode, rangeNode)
                    .Where(t => t.Object.NodeType == NodeType.Uri)
                    .Select(t => ((IUriNode)t.Object).Uri)
                    .SingleOrDefault();
                if (rangeUri == null)
                {
                    
                }
                string propertyTypeName = null;
                if (rangeUri == null)
                    propertyTypeName = xsdTypeDictionary[stringXsdUri.ToString()];
                else
                    if (xsdTypeDictionary.ContainsKey(rangeUri.ToString()))
                {
                    string typeName = xsdTypeDictionary[rangeUri.ToString()];
                    if ((isFunctional) && (typeName != typeof(string).Name))
                        propertyTypeName = $"{typeName}?";
                    else
                        propertyTypeName = typeName;
                }
                else
                    propertyTypeName = $"I{rangeUri.GetNameFromUriNode(graph.NamespaceMap)}";
                CodeTypeReference propertyType = null;
                if (isFunctional)
                    propertyType = new CodeTypeReference(propertyTypeName);
                else
                    propertyType = new CodeTypeReference(typeof(IEnumerable<>).FullName, new CodeTypeReference[] { new CodeTypeReference(propertyTypeName) });

                CodeMemberProperty property = new CodeMemberProperty();
                property.Name = $"{name[0].ToString().ToUpper()}{name.Substring(1)}";
                property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                property.Type = propertyType;
                CodeAttributeDeclaration propertyPredicateAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(UriPredicateAttribute)));
                propertyPredicateAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(propertyNode.Uri.ToString())));
                property.CustomAttributes.Add(propertyPredicateAttribute);
                if (rangeUri != null)
                {
                    CodeAttributeDeclaration propertyRangeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(UriRangeAttribute)));
                    propertyRangeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(rangeUri.ToString())));
                    property.CustomAttributes.Add(propertyRangeAttribute);
                }
                property.HasGet = true;
                property.HasSet = true;

                yield return property;
            }
        }

        private static IEnumerable<CodeTypeMember> copyMembersFromInterface(CodeTypeMemberCollection members)
        {
            foreach (CodeTypeMember member in members)
            {
                CodeMemberField privateField = new CodeMemberField();
                privateField.Name = $"{member.Name[0].ToString().ToLower()}{member.Name.Substring(1)}";
                privateField.Attributes = MemberAttributes.Private;
                privateField.Type = ((CodeMemberProperty)member).Type;
                yield return privateField;

                CodeMemberProperty property = new CodeMemberProperty();
                property.Name = member.Name;
                property.Attributes = member.Attributes;
                property.Type = ((CodeMemberProperty)member).Type;
                property.GetStatements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(privateField.Name)));
                property.SetStatements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression(privateField.Name), new CodePropertySetValueReferenceExpression()));
                yield return property;
            }
        }

        /*private Uri findUnderlyingRange(IGraph graph, INode propertyNode)
        {
            IUriNode rangeNode = graph.CreateUriNode("rdfs:range");
            IUriNode subPropertyNode = graph.CreateUriNode("rdfs:subPropertyOf");

            foreach (Triple triple in graph.GetTriplesWithSubjectPredicate(propertyNode, subPropertyNode))
            {
                Triple rangeTriple = graph.GetTriplesWithSubjectObject(triple.Object, rangeNode).FirstOrDefault();
                if (rangeTriple!=null)
                    return (IUriNode)rangeTriple.Object
            }
                        .Where(t => graph.GetTriplesWithSubjectObject(t.Object, rangeNode)
        }*/

        private Dictionary<string, string> xsdTypeDictionary = new Dictionary<string, string>
        {
            { "http://www.w3.org/2001/XMLSchema#date", nameof(DateTimeOffset) },
            { "http://www.w3.org/2001/XMLSchema#dateTime", nameof(DateTimeOffset) },
            { "http://www.w3.org/2001/XMLSchema#integer", typeof(int).Name },
            { "http://www.w3.org/2001/XMLSchema#decimal", typeof(decimal).Name },
            { "http://www.w3.org/2001/XMLSchema#double", typeof(double).Name },
            { "http://www.w3.org/2001/XMLSchema#boolean", typeof(bool).Name },
            { "http://www.w3.org/2001/XMLSchema#string", typeof(string).Name},
            { "http://www.opengis.net/ont/geosparql#wktLiteral", typeof(string).Name}
        };
    }
}
