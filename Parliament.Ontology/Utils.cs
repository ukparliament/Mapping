namespace Parliament.Ontology
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Linq;
    using VDS.RDF;
    using VDS.RDF.Ontology;
    using VDS.RDF.Parsing;

    internal static class Utils
    {
        internal static string Lowercase(this string original)
        {
            var result = original.ToCharArray();
            result[0] = char.ToLower(result[0]);
            return new string(result);
        }

        internal static string Uppercase(this string original)
        {
            var result = original.ToCharArray();
            result[0] = char.ToUpper(result[0]);
            return new string(result);
        }

        internal static string ToInterfaceName(this OntologyClass ontologyClass)
        {
            return string.Concat("I", ontologyClass.ToPascalCase());
        }

        internal static string ToPascalCase(this OntologyResource ontologyResource)
        {
            return ontologyResource.ToMemberName().Uppercase();
        }

        internal static string ToMemberName(this OntologyResource ontologyResource)
        {
            var uri = ontologyResource.ToUri();
            var namespaceMapper = ontologyResource.Graph.NamespaceMap;

            // TODO: Review neccessity of prefixes. At the moment it's only for skos:Concept, which seems redundant.
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
                prefix = prefix.Uppercase();
            }

            return string.Concat(prefix, name);
        }

        private static Dictionary<string, Type> XsdTypeDictionary = new Dictionary<string, Type>
        {
            { "http://www.w3.org/2001/XMLSchema#date", typeof(DateTimeOffset) },
            { "http://www.w3.org/2001/XMLSchema#dateTime", typeof(DateTimeOffset) },
            { "http://www.w3.org/2001/XMLSchema#integer", typeof(int) },
            { "http://www.w3.org/2001/XMLSchema#decimal", typeof(decimal) },
            { "http://www.w3.org/2001/XMLSchema#double", typeof(double) },
            { "http://www.w3.org/2001/XMLSchema#boolean", typeof(bool) },
            { "http://www.w3.org/2001/XMLSchema#string", typeof(string) },
            { "http://www.opengis.net/ont/geosparql#wktLiteral", typeof(string) }
        };

        internal static CodeTypeReference ToTypeReference(this OntologyProperty ontologyProperty, bool isInterfaceTypeAllowed)
        {
            var rangeClass = ontologyProperty.Ranges.FirstOrDefault();
            var isFunctional = ontologyProperty.IsFunctional();
            var result = null as CodeTypeReference;

            if (rangeClass == null)
            {
                result = new CodeTypeReference(typeof(string));
            }
            else if (Utils.XsdTypeDictionary.TryGetValue(rangeClass.ToUri().AbsoluteUri, out Type mappedType))
            {
                if (isFunctional && mappedType.IsValueType)
                {
                    result = new CodeTypeReference(typeof(Nullable<>).MakeGenericType(mappedType));
                }
                else
                {
                    result = new CodeTypeReference(mappedType);
                }
            }
            else
            {
                if (isInterfaceTypeAllowed)
                    result = new CodeTypeReference(rangeClass.ToInterfaceName());
                else
                    result = new CodeTypeReference(rangeClass.ToMemberName());
            }

            if (!isFunctional)
            {
                var enumerable = new CodeTypeReference(typeof(IEnumerable<>));
                enumerable.TypeArguments.Add(result);

                return enumerable;
            }

            return result;
        }

        private static bool IsFunctional(this OntologyProperty ontologyProperty)
        {
            var functionalTriple = new Triple(
                ontologyProperty.Resource,
                ontologyProperty.Graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)),
                ontologyProperty.Graph.CreateUriNode("owl:FunctionalProperty"));

            return ontologyProperty.Graph.ContainsTriple(functionalTriple);
        }

        internal static Uri ToUri(this OntologyResource ontologyResource)
        {
            return (ontologyResource.Resource as IUriNode).Uri;
        }
    }
}
