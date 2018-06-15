namespace Parliament.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class ClassAttribute : ResourceAttribute
    {
        public ClassAttribute(string typeUri) : base(typeUri) { }
    }
}
