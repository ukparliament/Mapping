using System;
using System.Linq;
using VDS.RDF;

namespace Parliament.Ontology.Mapper
{
    internal static class UriExtensions
    {
        public static string GetNameFromUriNode(this Uri uri, INamespaceMapper namespaceMapper)
        {
            string prefix = null;
            string name = null;

            if (namespaceMapper.ReduceToQName(uri.AbsoluteUri, out string qname))
                prefix = qname.Split(':')[0];

            if (string.IsNullOrWhiteSpace(uri.Fragment))
                name = uri.Segments.LastOrDefault();
            else
                name = uri.Fragment.Substring(1);
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = string.Empty;
            else
                prefix = $"{prefix[0].ToString().ToUpper()}{prefix.Substring(1)}";
            return $"{prefix}{name}";
        }
    }
}
