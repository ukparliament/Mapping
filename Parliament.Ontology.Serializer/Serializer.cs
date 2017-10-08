namespace Parliament.Ontology.Serializer
{
    using Parliament.Ontology.Base;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using VDS.RDF;
    using VDS.RDF.Parsing;

    public class Serializer
    {
        private string idPropertyName = nameof(IOntologyInstance.SubjectUri);

        // TODO: Is generic string typing required here?
        public Graph Serialize<T>(IEnumerable<T> items, Assembly ontologyAssembly, SerializerOptions serializerOptions = SerializerOptions.None, Graph graph = null) where T : IOntologyInstance
        {
            if ((items == null) || (items.Any() == false))
            {
                return null;
            }

            if (graph == null)
            {
                graph = new Graph();
            }

            var propertyMetadataDictionary = giveMePropertyMetadataDictionary(ontologyAssembly);
            var classUriTypeDictionary = giveMeClassUriTypeDictionary(ontologyAssembly);

            foreach (var item in items)
            {
                var idValue = item.GetType().GetProperty(idPropertyName).GetValue(item, null);
                if (idValue == null)
                {
                    throw new NullReferenceException(idPropertyName);
                }

                var id = new Uri(idValue.ToString());
                var triples = generateGraph(item, id, propertyMetadataDictionary, classUriTypeDictionary, serializerOptions, new Dictionary<Uri, HashSet<Uri>>());

                graph.Assert(triples);
            }

            return graph;
        }

        public IEnumerable<IOntologyInstance> Deserialize(IGraph graph, Assembly ontologyAssembly)
        {
            Dictionary<Type, Uri> classUriTypeDictionary = giveMeClassUriTypeDictionary(ontologyAssembly);
            IOntologyInstance[] things = giveMeSomeThings(graph, classUriTypeDictionary).ToArray();
            Dictionary<string, PropertyMetadata> propertyMetadataDictionary = giveMePropertyMetadataDictionary(ontologyAssembly);
            foreach (IOntologyInstance item in things)
                populateInstance(item, things, graph, propertyMetadataDictionary);
            return things;
        }

        private IEnumerable<Triple> generateGraph(object item, Uri id, Dictionary<string, PropertyMetadata> propertyMetadataDictionary, Dictionary<Type, Uri> classUriTypeDictionary, SerializerOptions serializerOptions, Dictionary<Uri, HashSet<Uri>> serializedSubjects)
        {
            TripleCollection result = new TripleCollection();
            NodeFactory nodeFactory = new NodeFactory();
            List<Triple> triples = new List<Triple>();
            Type type = item.GetType();
            IUriNode subjectNode = nodeFactory.CreateUriNode(id);

            Uri subjectType = classUriTypeDictionary[type];
            if ((serializedSubjects.ContainsKey(subjectNode.Uri)) && (serializedSubjects[subjectNode.Uri].Contains(subjectType)))
                return triples;
            if (serializerOptions != SerializerOptions.ExcludeRdfType)
                triples.Add(new Triple(subjectNode, nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), nodeFactory.CreateUriNode(subjectType)));
            if (serializedSubjects.ContainsKey(subjectNode.Uri))
                serializedSubjects[subjectNode.Uri].Add(subjectType);
            else
                serializedSubjects.Add(subjectNode.Uri, new HashSet<Uri>() { subjectType });
            foreach (string propertyName in giveMePropertyNames(type))
            {
                PropertyMetadata propertyMetadata = propertyMetadataDictionary[propertyName];
                object propertyValue = type.GetProperty(propertyName).GetValue(item, null);
                if (propertyValue != null)
                {
                    IEnumerable<object> propertyValues = null;
                    if (isTypeEnumerable(propertyValue.GetType()))
                        propertyValues = (propertyValue as IEnumerable).Cast<object>();
                    else
                        propertyValues = new object[] { propertyValue };
                    foreach (object itemValue in propertyValues)
                    {
                        if (propertyMetadata.IsComplexType)
                        {
                            Uri childId = new Uri(itemValue.GetType().GetProperty(idPropertyName).GetValue(itemValue, null).ToString());
                            triples.Add(new Triple(subjectNode, nodeFactory.CreateUriNode(propertyMetadata.PredicateUri), nodeFactory.CreateUriNode(childId)));
                            triples.AddRange(generateGraph(itemValue, childId, propertyMetadataDictionary, classUriTypeDictionary, serializerOptions, serializedSubjects));
                        }
                        else
                        {
                            ILiteralNode valueNode = null;
                            if (propertyMetadata.ObjectRangeUri != null)
                            {
                                if (propertyMetadata.ObjectRangeUri.ToString() == "http://www.w3.org/2001/XMLSchema#date")
                                    valueNode = ((DateTimeOffset)itemValue).ToLiteralDate(nodeFactory);
                                else
                                    if (propertyMetadata.ObjectRangeUri.ToString() == "http://www.w3.org/2001/XMLSchema#string")
                                    valueNode = nodeFactory.CreateLiteralNode(itemValue.ToString());
                                else
                                            if (propertyMetadata.ObjectRangeUri.ToString() == "http://www.w3.org/2001/XMLSchema#dateTime")
                                    valueNode = ((DateTimeOffset)itemValue).ToLiteral(nodeFactory, true);
                                else
                                    valueNode = nodeFactory.CreateLiteralNode(itemValue.ToString(), propertyMetadata.ObjectRangeUri);
                            }
                            else
                                valueNode = nodeFactory.CreateLiteralNode(itemValue.ToString());
                            triples.Add(new Triple(subjectNode, nodeFactory.CreateUriNode(propertyMetadata.PredicateUri), valueNode));
                        }
                    }
                }
            }

            return triples;
        }

        private IEnumerable<IOntologyInstance> giveMeSomeThings(IGraph graph, Dictionary<Type, Uri> classUriTypeDictionary)
        {
            IEnumerable<Triple> thingNodes = graph.GetTriplesWithPredicate(graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)))
                .Distinct();
            foreach (Triple thing in thingNodes)
            {
                Type classType = classUriTypeDictionary.FirstOrDefault(cu => cu.Value == ((IUriNode)thing.Object).Uri).Key;
                object result = Activator.CreateInstance(classType);
                ((IOntologyInstance)result).SubjectUri = ((IUriNode)thing.Subject).Uri;
                yield return result as IOntologyInstance;
            }
        }

        private Dictionary<Type, Uri> giveMeClassUriTypeDictionary(Assembly ontologyAssembly)
        {
            return ontologyAssembly.GetTypes()
                .Where(t => t.IsClass && t.GetInterfaces().Any(i => i == typeof(IOntologyInstance)))
                .Select(t => new KeyValuePair<Type, Type>(t, t.GetInterfaces()
                    .Except(new Type[] { typeof(IOntologyInstance) })
                    .Except(t.GetInterfaces().SelectMany(i => i.GetInterfaces()))
                    .SingleOrDefault()))
                .Select(ic => new KeyValuePair<Type, Uri>(ic.Key, ic.Value.GetCustomAttribute<ClassAttribute>().Uri))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private Dictionary<string, PropertyMetadata> giveMePropertyMetadataDictionary(Assembly ontologyAssembly)
        {
            return ontologyAssembly.GetTypes()
                .Where(t => t.IsInterface && t.GetInterfaces().Any(i => i == typeof(IOntologyInstance)))
                .SelectMany(i => i.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty))
                .Select(p => new KeyValuePair<string, PropertyMetadata>(p.Name, new PropertyMetadata()
                {
                    PredicateUri = p.GetCustomAttribute<PropertyAttribute>().Uri,
                    // TODO: Get ObjectRangeUri from type interface declaration class attribute?
                    ObjectRangeUri = p.GetCustomAttribute<RangeAttribute>() != null ? p.GetCustomAttribute<RangeAttribute>().Uri : null,
                    IsComplexType = (p.PropertyType.IsPrimitive == false) && (p.PropertyType.IsValueType == false) && (p.PropertyType != typeof(string)) &&
                          (p.PropertyType.GenericTypeArguments.All(t => t.IsPrimitive == false)) && (p.PropertyType.GenericTypeArguments.All(t => t.IsValueType == false)) && (p.PropertyType.GenericTypeArguments.All(t => t != typeof(string)))
                }))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private IEnumerable<string> giveMePropertyNames(Type t)
        {
            PropertyInfo[] properties = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            return properties.Select(p => p.Name).Where(n => n != idPropertyName);
        }

        private void populateInstance(IOntologyInstance item, IOntologyInstance[] things, IGraph graph, Dictionary<string, PropertyMetadata> propertyMetadataDictionary)
        {
            IUriNode subject = graph.CreateUriNode(item.SubjectUri);
            IEnumerable<Triple> triples = graph.GetTriplesWithSubject(subject)
                .Where(t => ((IUriNode)t.Predicate).Uri.ToString() != RdfSpecsHelper.RdfType);
            IEnumerable<IUriNode> predicatesValues = triples
                .Select(t => (IUriNode)t.Predicate)
                .Distinct();
            Dictionary<Uri, string> predicatePropertyNameDictionary = propertyMetadataDictionary.Select(pm => new KeyValuePair<Uri, string>(pm.Value.PredicateUri, pm.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (IUriNode predicate in predicatesValues)
            {
                IEnumerable<object> values = triples.WithPredicate(predicate)
                    .Select(t => getTypedValueFromNode(t.Object, things))
                    .Where(v => v != null);
                if ((values != null) && (values.Any()))
                {
                    if (predicatePropertyNameDictionary.ContainsKey(predicate.Uri) == false)
                        continue;
                    string propertyName = predicatePropertyNameDictionary[predicate.Uri];
                    if (string.IsNullOrEmpty(propertyName))
                        continue;
                    PropertyInfo propertyInfo = item.GetType().GetProperty(propertyName);
                    if (propertyInfo == null)
                        continue;
                    PropertyMetadata propertyMetadata = propertyMetadataDictionary[propertyName];
                    if (isTypeEnumerable(propertyInfo.PropertyType))
                    {
                        MethodInfo castMethodValues = null;
                        object castValues = null;
                        Type castType = null;
                        castType = values.FirstOrDefault().GetType();
                        castMethodValues = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(castType);
                        castValues = castMethodValues.Invoke(values, new object[] { values });
                        if (propertyInfo.PropertyType.GenericTypeArguments.All(t => castValues.GetType().GenericTypeArguments.Any(c => c.GetInterfaces().Contains(t)) || castValues.GetType().GenericTypeArguments.Contains(t)))
                            propertyInfo.SetValue(item, castValues);
                    }
                    else
                        if ((propertyInfo.PropertyType == values.SingleOrDefault().GetType()) ||
                            (propertyInfo.PropertyType.GenericTypeArguments.Any(t => t == values.SingleOrDefault().GetType())) ||
                            (values.SingleOrDefault().GetType().GetInterfaces().Any(i => i == propertyInfo.PropertyType)))
                        propertyInfo.SetValue(item, values.SingleOrDefault());
                }
            }
        }

        private object getTypedValueFromNode(INode node, IOntologyInstance[] things)
        {
            if (node is ILiteralNode)
            {
                ILiteralNode literalNode = (ILiteralNode)node;
                TypeCode typeCode = xsdTypeDictionary[literalNode.DataType == null ? "http://www.w3.org/2001/XMLSchema#string" : literalNode.DataType.ToString()];
                if (typeCode == TypeCode.DateTime)
                    return DateTimeOffset.Parse(literalNode.Value);
                else
                    return Convert.ChangeType(literalNode.Value, typeCode);
            }
            else
            if (node is IUriNode)
            {
                IUriNode uriNode = (IUriNode)node;
                return things.SingleOrDefault(t => t.SubjectUri == uriNode.Uri);
            }
            else
                return null;
        }

        private Dictionary<string, TypeCode> xsdTypeDictionary
        {
            get
            {
                return new Dictionary<string, TypeCode>
                {
                    { "http://www.w3.org/2001/XMLSchema#date", TypeCode.DateTime },
                    { "http://www.w3.org/2001/XMLSchema#dateTime", TypeCode.DateTime },
                    { "http://www.w3.org/2001/XMLSchema#integer", TypeCode.Int32 },
                    { "http://www.w3.org/2001/XMLSchema#decimal", TypeCode.Decimal },
                    { "http://www.w3.org/2001/XMLSchema#double", TypeCode.Double },
                    { "http://www.w3.org/2001/XMLSchema#string", TypeCode.String },
                    { "http://www.opengis.net/ont/geosparql#wktLiteral", TypeCode.String }
                };
            }
        }

        private bool isTypeEnumerable(Type type)
        {
            return (type != typeof(string)) && ((type.IsArray) ||
                ((type.GetInterfaces().Any()) && (type.GetInterfaces().Any(i => i == typeof(IEnumerable)))));
        }
    }
}
