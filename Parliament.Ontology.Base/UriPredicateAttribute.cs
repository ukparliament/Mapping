namespace Parliament.Ontology.Base
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class UriPredicateAttribute : UriAttribute
    {
        public UriPredicateAttribute(string predicateUri) : base(predicateUri) { }
    }
}
