namespace Parliament.Rdf
{
    using System;

    public interface IResource
    {
        Uri Id { get; set; }
        Uri BaseUri { get; set; }
        string LocalId { get; }
    }
}