namespace Parliament.Ontology.Base
{
    using System;

    [AttributeUsage(AttributeTargets.Interface)]
    public class UriTypeAttribute : UriAttribute
    {
        public UriTypeAttribute(string typeUri) : base(typeUri) { }
    }
}
