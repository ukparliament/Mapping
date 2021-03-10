using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parliament.Model;
using Parliament.Ontology;
using Parliament.Rdf.Serialization;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Storage;

namespace Mapping.Test
{
    [TestClass]
    public class DeserializationTest
    {

        private readonly CompilerResults compilerResults;

        public DeserializationTest()
        {
            compilerResults = ModelGenerator.CompileModelImplementationAssembly("Ontology.ttl", "Parliament.Model", System.IO.Path.GetTempPath());
        }

        [TestMethod]
        public void DeserializeTest()
        {
            var Sparql = new SparqlParameterizedString(ExistingGraphSparqlCommand);
            Sparql.Namespaces.AddNamespace("parl", new Uri("https://id.parliament.uk/schema/"));
            Dictionary<string, INode> graphRetrievalDictionary = new Dictionary<string, INode>();
            NodeFactory nodeFactory = new NodeFactory();
            graphRetrievalDictionary.Add("subject", nodeFactory.CreateUriNode(new Uri("https://id.parliament.uk/WyfZUQ8R")));
            string sparqlEndpoint = "https://api.parliament.uk/sparql";
            string nameSpace = "https://id.parliament.uk/";
            Sparql.SetParameter("subject", nodeFactory.CreateUriNode(new Uri("https://id.parliament.uk/WyfZUQ8R")));
            var graph = Execute(Sparql.ToString(), sparqlEndpoint, nameSpace);

            RdfSerializer serializer = new RdfSerializer();
            IEnumerable<BaseResource> ontologyInstances = serializer.Deserialize(graph, typeof(Person).Assembly, new Uri(nameSpace));
        }
        public static IGraph Execute(string queryString, string sparqlEndpoint, string nameSpace)
        {
            IGraph graph = null;
            using (var connector = new SparqlConnector(new Uri(sparqlEndpoint)))
            {
                graph = connector.Query(queryString) as IGraph;
            }

            return graph;
        }

        public string ExistingGraphSparqlCommand
        {
            get
            {
                return @"
            construct {
                ?ePetition a parl:EPetition;
		            parl:ePetitionUkgapId ?ePetitionUkgapId;
	                parl:background ?background;
	                parl:additionalDetails ?additionalDetails;
	                parl:closedAt ?closedAt;
                    parl:createdAt ?createdAt;
	                parl:action ?action;
	                parl:updatedAt ?updatedAt;
	                parl:ePetitionHasGovernmentResponse ?governmentResponse;
                    parl:ePetitionHasDebate ?debate;
                    parl:ePetitionHasModeration ?moderation;
                    parl:ePetitionHasThresholdAttainment ?thresholdAttainment.
		        ?governmentResponse a parl:GovernmentResponse;
                    parl:governmentResponseCreatedAt ?governmentResponseCreatedAt;
		            parl:governmentResponseUpdatedAt ?governmentResponseUpdatedAt;
		            parl:governmentResponseDetails ?governmentResponseDetails;
		            parl:governmentResponseSummary ?governmentResponseSummary.
                ?debate a parl:Debate;
		            parl:debateProposedDate ?debateProposedDate;
                    parl:debateDate ?debateDate;
                    parl:debateVideoUrl ?debateVideoUrl;
                    parl:debateTranscriptUrl ?debateTranscriptUrl;
                    parl:debateOverview ?debateOverview.
                ?rejection a parl:Rejection; 
                    parl:rejectionHasRejectionCode ?rejectionCode ;
                    parl:rejectionDetails ?rejectionDetails ;
                    parl:rejectedAt ?rejectedAt.
                ?approval a parl:Approval; 
                    parl:approvedAt ?approvedAt.
                ?thresholdAttainment a parl:ThresholdAttainment;
                    parl:thresholdAttainmentAt ?thresholdAttainmentAt;
                    parl:thresholdAttainmentHasThreshold ?threshold.
            }
            where {
	            bind(@subject as ?ePetition)
	            ?ePetition parl:ePetitionUkgapId ?ePetitionUkgapId.
	            optional {?ePetition parl:background ?background}
	            optional {?ePetition parl:additionalDetails ?additionalDetails}
                optional {?ePetition parl:createdAt ?createdAt}
	            optional {?ePetition parl:closedAt ?closedAt}
	            optional {?ePetition parl:action ?action}
                optional {?ePetition parl:updatedAt ?updatedAt}
                optional {
		            ?ePetition parl:ePetitionHasGovernmentResponse ?governmentResponse
		            optional {?governmentResponse parl:governmentResponseCreatedAt ?governmentResponseCreatedAt}
		            optional {?governmentResponse parl:governmentResponseUpdatedAt ?governmentResponseUpdatedAt}
		            optional {?governmentResponse parl:governmentResponseDetails ?governmentResponseDetails}
		            optional {?governmentResponse parl:governmentResponseSummary ?governmentResponseSummary}
	            }
		        optional {
		            ?ePetition parl:ePetitionHasDebate ?debate
		            optional {?debate parl:debateProposedDate ?debateProposedDate}
                    optional {?debate parl:debateDate ?debateDate}
                    optional {?debate parl:debateVideoUrl ?debateVideoUrl}
                    optional {?debate parl:debateTranscriptUrl ?debateTranscriptUrl}
                    optional {?debate parl:debateOverview ?debateOverview}
	            }
                optional {
                    ?ePetition parl:ePetitionHasModeration ?moderation.
                    optional {
                        bind(?moderation as ?rejection)
                        ?rejection parl:rejectedAt ?rejectedAt.
                        optional {?rejection parl:rejectionHasRejectionCode ?rejectionCode}
                        optional {?rejection parl:rejectionDetails ?rejectionDetails}
                    }
                    optional {
                        bind(?moderation as ?approval)
                        ?approval parl:approvedAt ?approvedAt.
                    }
                }
                optional {
                    ?ePetition parl:ePetitionHasThresholdAttainment ?thresholdAttainment
                    optional {?thresholdAttainment parl:thresholdAttainmentAt ?thresholdAttainmentAt}
                    optional {?thresholdAttainment parl:thresholdAttainmentHasThreshold ?threshold}
                }
            }";
            }
        }
    }
}
