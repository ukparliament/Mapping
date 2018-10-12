namespace Parliament.Rdf.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using VDS.RDF;
    using VDS.RDF.Parsing;

    public class RdfSerializer
    {
        private string idPropertyName = nameof(BaseResource.Id);
        private string baseUriPropertyName = nameof(BaseResource.BaseUri);
        private string localIdPropertyName = nameof(BaseResource.LocalId);

        // TODO: Is generic string typing required here?
        public Graph Serialize<T>(IEnumerable<T> items, Type[] model, SerializerOptions serializerOptions = SerializerOptions.None, Graph graph = null) where T : BaseResource
        {
            if ((items == null) || (items.Any() == false))
            {
                return null;
            }

            if (graph == null)
            {
                graph = new Graph();
            }

            var propertyMetadataDictionary = giveMePropertyMetadataDictionary(model);
            var classUriTypeDictionary = giveMeClassUriTypeDictionary(model);

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

        public IEnumerable<BaseResource> Deserialize(IGraph graph, Type[] model, Uri baseUri = null)
        {
            Dictionary<Type, Uri> classUriTypeDictionary = giveMeClassUriTypeDictionary(model);
            BaseResource[] things = giveMeSomeThings(graph, classUriTypeDictionary, baseUri).ToArray();
            Dictionary<string, PropertyMetadata> propertyMetadataDictionary = giveMePropertyMetadataDictionary(model);
            foreach (BaseResource item in things)
                populateInstance(item, things, graph, propertyMetadataDictionary);
            return things;
        }

        public IEnumerable<BaseResource> Deserialize(IGraph graph, Assembly modelAssembly, Uri baseUri = null)
        {
            return Deserialize(graph, modelAssembly.GetTypes(), baseUri);
        }

        private IEnumerable<Triple> generateGraph(object item, Uri id, Dictionary<string, PropertyMetadata> propertyMetadataDictionary, Dictionary<Type, Uri> classUriTypeDictionary, SerializerOptions serializerOptions, Dictionary<Uri, HashSet<Uri>> serializedSubjects)
        {
            TripleCollection result = new TripleCollection();
            NodeFactory nodeFactory = new NodeFactory();
            List<Triple> triples = new List<Triple>();
            Type type = item.GetType();
            IUriNode subjectNode = nodeFactory.CreateUriNode(id);

            Uri subjectType = classUriTypeDictionary[type];
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
                                {
                                    DateTimeOffset dt = (DateTimeOffset)itemValue;
                                    dt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dt, "GMT Standard Time");
                                    valueNode = dt.ToLiteralDate(nodeFactory);
                                }
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

            return triples.Distinct();
        }

        private IEnumerable<BaseResource> giveMeSomeThings(IGraph graph, Dictionary<Type, Uri> classUriTypeDictionary, Uri baseUri = null)
        {
            IEnumerable<Triple> thingNodes = graph.GetTriplesWithPredicate(graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)))
                .Distinct();
            foreach (Triple thing in thingNodes)
            {
                Type classType = classUriTypeDictionary.FirstOrDefault(cu => cu.Value == ((IUriNode)thing.Object).Uri).Key;
                object result = Activator.CreateInstance(classType);
                ((BaseResource)result).Id = ((IUriNode)thing.Subject).Uri;
                ((BaseResource)result).BaseUri = baseUri;
                yield return result as BaseResource;
            }
        }

        private Dictionary<Type, Uri> giveMeClassUriTypeDictionary(Type[] model)
        {
            return model
                .Where(t => t.IsClass && t.BaseType == typeof(BaseResource))
                .Select(t => new KeyValuePair<Type, Uri>(t, t.GetCustomAttribute<ClassAttribute>().Uri))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private Dictionary<string, PropertyMetadata> giveMePropertyMetadataDictionary(Type[] model)
        {
            return model
                .Where(t => t.IsClass && t.BaseType == typeof(BaseResource))
                .SelectMany(i => i.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty))
                .Select(p => new KeyValuePair<string, PropertyMetadata>(p.Name, new PropertyMetadata()
                {
                    PredicateUri = p.GetCustomAttribute<PropertyAttribute>().Uri,
                    ObjectRangeUri = p.GetCustomAttribute<RangeAttribute>() != null ? p.GetCustomAttribute<RangeAttribute>().Uri : null,
                    IsComplexType = (p.PropertyType.IsPrimitive == false) && (p.PropertyType.IsValueType == false) && (p.PropertyType != typeof(string)) &&
                          (p.PropertyType.GenericTypeArguments.All(t => t.IsPrimitive == false)) && (p.PropertyType.GenericTypeArguments.All(t => t.IsValueType == false)) && (p.PropertyType.GenericTypeArguments.All(t => t != typeof(string)))
                }))
                .Distinct()
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private IEnumerable<string> giveMePropertyNames(Type t)
        {
            PropertyInfo[] properties = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            return properties
                .Select(p => p.Name)
                .Where(n => (n != idPropertyName) && (n != localIdPropertyName) && (n != baseUriPropertyName));
        }

        private void populateInstance(BaseResource item, BaseResource[] things, IGraph graph, Dictionary<string, PropertyMetadata> propertyMetadataDictionary)
        {
            IUriNode subject = graph.CreateUriNode(item.Id);
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

        private object getTypedValueFromNode(INode node, BaseResource[] things)
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
                return things.SingleOrDefault(t => t.Id == uriNode.Uri);
            }
            else
                return null;
        }

        /// <summary>
        /// gYear conversion is to integer - no timezone offset
        /// </summary>
        private Dictionary<string, TypeCode> xsdTypeDictionary
        {
            get
            {
                return new Dictionary<string, TypeCode>
                {
                    { "http://www.w3.org/2001/XMLSchema#date", TypeCode.DateTime },
                    { "http://www.w3.org/2001/XMLSchema#dateTime", TypeCode.DateTime },
                    { "http://www.w3.org/2001/XMLSchema#gYear", TypeCode.Int32 },
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
