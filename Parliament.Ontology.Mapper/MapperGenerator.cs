namespace Parliament.Ontology.Mapper
{
    using Microsoft.CSharp;
    using Parliament.Ontology.Base;
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using VDS.RDF;
    using VDS.RDF.Parsing;

    public static class MapperGenerator
    {
        private static Dictionary<string, string> xsdTypeDictionary = new Dictionary<string, string>
        {
            { "http://www.w3.org/2001/XMLSchema#date", nameof(DateTimeOffset) },
            { "http://www.w3.org/2001/XMLSchema#dateTime", nameof(DateTimeOffset) },
            { "http://www.w3.org/2001/XMLSchema#integer", nameof(Int32) },
            { "http://www.w3.org/2001/XMLSchema#decimal", nameof(Decimal) },
            { "http://www.w3.org/2001/XMLSchema#double", nameof(Double) },
            { "http://www.w3.org/2001/XMLSchema#boolean", nameof(Boolean) },
            { "http://www.w3.org/2001/XMLSchema#string", nameof(String)},
            { "http://www.opengis.net/ont/geosparql#wktLiteral", nameof(String)}
        };

        public static CompilerResults CompileAssembly(string ontologyFilePath, string namespaceName)
        {
            var dom = MapperGenerator.GenerateCodeDom(ontologyFilePath, namespaceName);
            var parameters = new CompilerParameters()
            {
                OutputAssembly = $"{namespaceName}.dll"
            };

            using (var provider = new CSharpCodeProvider())
            {
                return provider.CompileAssemblyFromDom(parameters, dom);
            }
        }

        public static string GenerateCode(string ontologyFilePath, string namespaceName)
        {
            var dom = MapperGenerator.GenerateCodeDom(ontologyFilePath, namespaceName);

            using (var provider = new CSharpCodeProvider())
            {
                using (var writer = new StringWriter())
                {
                    provider.GenerateCodeFromCompileUnit(dom, writer, null);

                    return writer.ToString();
                }
            }
        }

        private static CodeCompileUnit GenerateCodeDom(string ontologyFilePath, string namespaceName)
        {
            var compileUnit = new CodeCompileUnit();
            var namespaceCode = new CodeNamespace(namespaceName);

            namespaceCode.Imports.Add(new CodeNamespaceImport(nameof(System)));
            compileUnit.ReferencedAssemblies.Add(typeof(Uri).Assembly.ManifestModule.Name);
            compileUnit.ReferencedAssemblies.Add(typeof(IOntologyInstance).Assembly.ManifestModule.Name);
            compileUnit.Namespaces.Add(namespaceCode);

            var graph = new Graph();
            graph.LoadFromFile(ontologyFilePath);

            var classNodes = graph.GetTriplesWithPredicateObject(graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), graph.CreateUriNode("owl:Class"))
                .Union(graph.GetTriplesWithPredicateObject(graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), graph.CreateUriNode("rdfs:subClassOf")))
                .Select(t => (IUriNode)t.Subject)
                .Distinct();

            foreach (var classNode in classNodes)
            {
                var interfaceCode = generateInterface(graph, classNode);
                namespaceCode.Types.Add(interfaceCode);
            }

            var idProperty = generateIdProperty();

            foreach (var classNode in classNodes)
            {
                var className = MakeClassName(classNode.Uri, graph.NamespaceMap);
                var classCode = new CodeTypeDeclaration(className);
                classCode.TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;

                foreach (CodeTypeDeclaration type in namespaceCode.Types)
                {
                    if (type.Name == MakeInterfaceName(className))
                    {
                        classCode.BaseTypes.Add(type.Name);
                        classCode.Members.AddRange(idProperty.ToArray());
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

        private static IEnumerable<CodeTypeMember> generateIdProperty()
        {
            var idFieldName = nameof(IOntologyInstance.SubjectUri);
            var privateFiled = new CodeMemberField();
            privateFiled.Name = Lowercase(idFieldName);
            privateFiled.Attributes = MemberAttributes.Private;
            privateFiled.Type = new CodeTypeReference(typeof(Uri));

            yield return privateFiled;

            var property = new CodeMemberProperty();
            property.Name = idFieldName;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            property.Type = new CodeTypeReference(typeof(Uri));
            property.GetStatements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(privateFiled.Name)));
            property.SetStatements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression(privateFiled.Name), new CodePropertySetValueReferenceExpression()));

            yield return property;
        }

        private static CodeTypeDeclaration generateInterface(Graph graph, IUriNode classNode)
        {
            var interfaceCode = new CodeTypeDeclaration(MakeInterfaceName(MakeClassName(classNode.Uri, graph.NamespaceMap)));
            interfaceCode.TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Interface;
            var interfaceTypeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(UriTypeAttribute)));
            interfaceTypeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(classNode.Uri.ToString())));
            interfaceCode.CustomAttributes.Add(interfaceTypeAttribute);
            interfaceCode.BaseTypes.Add(typeof(IOntologyInstance));

            var ancestorNodes = getAllClassAncestors(graph, classNode)
                .Distinct();

            foreach (var ancestorNode in ancestorNodes)
            {
                var ancestorName = MakeInterfaceName(MakeClassName(ancestorNode.Uri, graph.NamespaceMap));
                interfaceCode.BaseTypes.Add(ancestorName);
            }

            var properties = giveMeProperties(graph, classNode).ToArray();
            interfaceCode.Members.AddRange(properties);

            return interfaceCode;
        }

        private static CodeTypeDeclaration generateJoinedInterface(Graph graph, IUriNode superClassNode)
        {
            var interfaceCode = new CodeTypeDeclaration(MakeInterfaceName($"{MakeClassName(superClassNode.Uri, graph.NamespaceMap)}Joined"));
            interfaceCode.TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Interface;
            var interfaceTypeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(UriTypeAttribute)));
            interfaceTypeAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(superClassNode.Uri.ToString())));
            interfaceCode.CustomAttributes.Add(interfaceTypeAttribute);
            interfaceCode.BaseTypes.Add(typeof(IOntologyInstance));

            var offsprings = getClassOffsprings(graph, superClassNode)
                .Distinct();

            foreach (var subsetNode in offsprings)
            {
                var ancestorName = MakeInterfaceName(MakeClassName(subsetNode.Uri, graph.NamespaceMap));
                interfaceCode.BaseTypes.Add(ancestorName);
            }

            return interfaceCode;
        }

        private static IEnumerable<IUriNode> getAllClassAncestors(IGraph graph, IUriNode classNode)
        {
            var ancestors = graph.GetTriplesWithSubjectPredicate(classNode, graph.CreateUriNode("rdfs:subClassOf"))
                .OfType<IUriNode>()
                .Distinct();

            foreach (var ancestor in ancestors)
            {
                ancestors = ancestors.Concat(getAllClassAncestors(graph, ancestor));
            }

            return ancestors;
        }

        private static IEnumerable<IUriNode> getClassOffsprings(IGraph graph, IUriNode classNode)
        {
            var offsprings = graph.GetTriplesWithPredicateObject(graph.CreateUriNode("rdfs:subClassOf"), classNode)
                .OfType<IUriNode>()
                .Distinct();

            if (!offsprings.Any())
            {
                yield return classNode;
            }
            else
            {
                foreach (var offspring in offsprings)
                {
                    var nodes = getClassOffsprings(graph, offspring);

                    foreach (var node in nodes)
                    {
                        yield return node;
                    }
                }
            }
        }

        private static IEnumerable<CodeTypeMember> giveMeProperties(Graph graph, IUriNode classNode)
        {
            var stringXsdUri = new Uri("http://www.w3.org/2001/XMLSchema#string");
            var domainNode = graph.CreateUriNode("rdfs:domain");
            var rangeNode = graph.CreateUriNode("rdfs:range");

            var propertyNodes = graph.GetTriplesWithPredicateObject(domainNode, classNode)
                .Select(t => t.Subject as IUriNode)
                .Distinct();

            foreach (var propertyNode in propertyNodes)
            {
                var name = MakeClassName(propertyNode.Uri, graph.NamespaceMap);
                var functionalTriple = new Triple(propertyNode, graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), graph.CreateUriNode("owl:FunctionalProperty"));
                var isFunctional = graph.ContainsTriple(functionalTriple);
                var rangeUri = graph.GetTriplesWithSubjectPredicate(propertyNode, rangeNode)
                    .Select(x=>x.Object)
                    .OfType<IUriNode>()
                    .Select(t => t.Uri)
                    .SingleOrDefault();

                var propertyTypeName = null as string;
                if (rangeUri == null)
                {
                    propertyTypeName = xsdTypeDictionary[stringXsdUri.ToString()];
                }
                else if (xsdTypeDictionary.ContainsKey(rangeUri.ToString()))
                {
                    var typeName = xsdTypeDictionary[rangeUri.ToString()];
                    if (isFunctional && typeName != nameof(String))
                    {
                        propertyTypeName = $"{typeName}?";
                    }
                    else
                    {
                        propertyTypeName = typeName;
                    }
                }
                else
                {
                    propertyTypeName = MakeInterfaceName(MakeClassName(rangeUri, graph.NamespaceMap));
                }

                var propertyType = null as CodeTypeReference;

                if (isFunctional)
                {
                    propertyType = new CodeTypeReference(propertyTypeName);
                }
                else
                {
                    propertyType = new CodeTypeReference(typeof(IEnumerable<>).FullName, new CodeTypeReference[] { new CodeTypeReference(propertyTypeName) });
                }

                var property = new CodeMemberProperty();
                property.Name = Uppercase(name);
                property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                property.Type = propertyType;
                var propertyPredicateAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(UriPredicateAttribute)));
                propertyPredicateAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(propertyNode.Uri.ToString())));
                property.CustomAttributes.Add(propertyPredicateAttribute);
                if (rangeUri != null)
                {
                    var propertyRangeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(UriRangeAttribute)));
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
            foreach (CodeMemberProperty member in members)
            {
                var privateField = new CodeMemberField();
                privateField.Name = Lowercase(member.Name);
                privateField.Attributes = MemberAttributes.Private;
                privateField.Type = member.Type;

                yield return privateField;

                var property = new CodeMemberProperty();
                property.Name = member.Name;
                property.Attributes = member.Attributes;
                property.Type = member.Type;
                property.GetStatements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(privateField.Name)));
                property.SetStatements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression(privateField.Name), new CodePropertySetValueReferenceExpression()));

                yield return property;
            }
        }

        private static string MakeClassName(Uri uri, INamespaceMapper namespaceMapper)
        {
            var prefix = null as string;
            var name = null as string;

            if (namespaceMapper.ReduceToQName(uri.AbsoluteUri, out string qname))
            {
                prefix = qname.Split(':')[0];
            }

            if (string.IsNullOrWhiteSpace(uri.Fragment))
            {
                name = uri.Segments.LastOrDefault();
            }
            else
            {
                name = uri.Fragment.Substring(1);
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = string.Empty;
            }
            else
            {
                prefix = Uppercase(prefix);
            }

            return string.Concat(prefix, name);
        }

        private static string MakeInterfaceName(string className)
        {
            return string.Concat("I", className);
        }

        private static string Uppercase(string original)
        {
            var result = original.ToCharArray();
            result[0] = char.ToUpper(result[0]);
            return new string(result);
        }

        private static string Lowercase(string original)
        {
            var result = original.ToCharArray();
            result[0] = char.ToLower(result[0]);
            return new string(result);
        }
    }
}
