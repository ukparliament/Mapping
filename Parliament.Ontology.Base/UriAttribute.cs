using System;

namespace Parliament.Ontology.Base
{
    public class UriAttribute : Attribute
    {
        public UriAttribute(string uri)
        {
            UriItem = new Uri(uri);
        }

        protected Uri UriItem { get; private set; }
    }
}
