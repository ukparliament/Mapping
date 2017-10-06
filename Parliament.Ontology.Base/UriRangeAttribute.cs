namespace Parliament.Ontology.Base
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class UriRangeAttribute : UriAttribute
    {
        public UriRangeAttribute(string rangeUri) : base(rangeUri) { }
    }
}
