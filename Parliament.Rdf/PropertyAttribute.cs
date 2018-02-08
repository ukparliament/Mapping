namespace Parliament.Rdf
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyAttribute : ResourceAttribute
    {
        public PropertyAttribute(string predicateUri) : base(predicateUri) { }
    }
}
