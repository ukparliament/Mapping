using System;

namespace Parliament.Ontology.Base
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class UriTypeAttribute : UriAttribute
    {
        public UriTypeAttribute(string typeUri)
            : base(typeUri)
        {
        }

        public Uri TypeUri
        {
            get
            {
                return UriItem;
            }
        }
    }
}
