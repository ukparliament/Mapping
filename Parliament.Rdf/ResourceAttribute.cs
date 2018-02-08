namespace Parliament.Rdf
{
    using System;

    public abstract class ResourceAttribute : Attribute
    {
        private readonly Uri uri;

        public ResourceAttribute(string uri)
        {
            this.uri = new Uri(uri);
        }

        public Uri Uri
        {
            get
            {
                return this.uri;
            }
        }
    }
}
