namespace Parliament.Rdf
{
    using System;

    // TODO: Is this redundant? Could always get range uri from object property type interface declaration class attribute
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeAttribute : ResourceAttribute
    {
        public RangeAttribute(string rangeUri) : base(rangeUri) { }
    }
}
