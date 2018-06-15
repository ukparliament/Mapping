namespace Parliament.Rdf.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class RangeAttribute : ResourceAttribute
    {
        public RangeAttribute(string rangeUri) : base(rangeUri) { }
    }
}
