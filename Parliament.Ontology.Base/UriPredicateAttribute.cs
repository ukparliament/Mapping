using System;

namespace Parliament.Ontology.Base
{
    [AttributeUsage(AttributeTargets.Property)]
    public class UriPredicateAttribute : UriAttribute
    {
        public UriPredicateAttribute(string predicateUri)
            : base(predicateUri)
        {
        }

        public Uri PredicateUri
        {
            get
            {
                return UriItem;
            }
        }
    }
}
