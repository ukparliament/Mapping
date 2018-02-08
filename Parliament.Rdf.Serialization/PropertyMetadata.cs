using System;

namespace Parliament.Rdf.Serialization
{
    public struct PropertyMetadata
    {
        public Uri PredicateUri { get; set; }
        public Uri ObjectRangeUri { get; set; }
        public bool IsComplexType { get; set; }
    }
}
