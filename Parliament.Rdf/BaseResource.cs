using System;

namespace Parliament.Rdf
{
    public class BaseResource
    {
        public Uri Id { get; set; }
        public Uri BaseUri { get; set; }
        public string LocalId
        {
            get
            {
                if ((Id != null) && (BaseUri != null))
                {
                    return BaseUri.MakeRelativeUri(Id).ToString();
                }
                else
                    return null;
            }
        }
    }
}
