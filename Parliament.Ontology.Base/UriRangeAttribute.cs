using System;

namespace Parliament.Ontology.Base
{
    [AttributeUsage(AttributeTargets.Property)]
    public class UriRangeAttribute : UriAttribute
    {
        public UriRangeAttribute(string rangeUri)
            : base(rangeUri)
        {
        }

        public Uri RangeUri
        {
            get
            {
                return UriItem;
            }
        }
    }
}
