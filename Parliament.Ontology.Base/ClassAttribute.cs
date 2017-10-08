namespace Parliament.Ontology.Base
{
    using System;

    [AttributeUsage(AttributeTargets.Interface)]
    public class ClassAttribute : ResourceAttribute
    {
        public ClassAttribute(string typeUri) : base(typeUri) { }
    }
}
