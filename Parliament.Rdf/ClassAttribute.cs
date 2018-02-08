namespace Parliament.Rdf
{
    using System;

    [AttributeUsage(AttributeTargets.Interface)]
    public class ClassAttribute : ResourceAttribute
    {
        public ClassAttribute(string typeUri) : base(typeUri) { }
    }
}
