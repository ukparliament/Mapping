namespace Parliament.Ontology.Base
{
    using System;

    public abstract class UriAttribute : Attribute
    {
        public UriAttribute(string uri)
        {
            this.Uri = new Uri(uri);
        }

        public Uri Uri { get; private set; }
    }
}
