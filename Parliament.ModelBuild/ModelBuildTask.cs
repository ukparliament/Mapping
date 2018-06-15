using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Parliament.Ontology;
using System;
using System.CodeDom.Compiler;

namespace Parliament.ModelBuild
{
    public class ModelBuildTask : Task
    {
        public string OutputLocation { get; set; }

        [Required]
        public string Namespace { get; set; }

        [Required]
        public string OntologyFilePath { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(OutputLocation)==false)
                Log.LogMessage($"OutputLocation: {OutputLocation}");
            Log.LogMessage($"Namespace: {Namespace}");
            Log.LogMessage($"OntologyFilePath: {OntologyFilePath}");
            bool result = true;
            try
            {
                CompilerResults compilerResults = ModelGenerator.CompileModelImplementationAssembly(OntologyFilePath, Namespace, OutputLocation, false);
                Log.LogMessage($"{Namespace} compiled with {compilerResults.Errors.Count} error(s)");
                foreach (CompilerError error in compilerResults.Errors)
                    Log.LogError($"Error {error.ErrorNumber} ({error.Line},{error.Column}): {error.ErrorText}");
                result = compilerResults.Errors.HasErrors == false;
            }
            catch (Exception e)
            {
                Log.LogError(e.Message);
                result = false;
            }
            return result;
        }
    }
}
