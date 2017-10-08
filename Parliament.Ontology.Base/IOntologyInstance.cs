namespace Parliament.Ontology.Base
{
    using System;

    // TODO: This might be unneccessary (move to Thing base class instead).
    public interface IOntologyInstance
    {
        Uri SubjectUri { get; set; }
    }
}