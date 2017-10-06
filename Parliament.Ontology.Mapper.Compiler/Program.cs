namespace Parliament.Ontology.Mapper.Compiler
{
    using System;
    using System.CodeDom.Compiler;
    using System.Configuration;

    class Program
    {
        static void Main(string[] args)
        {
            var ontologyFilePath = ConfigurationManager.AppSettings["OntologyFilePath"];
            var namespaceName = ConfigurationManager.AppSettings["NamespaceName"];

            var result = MapperGenerator.CompileAssembly(ontologyFilePath, namespaceName);

            if (result.Errors.Count > 0)
            {
                foreach (CompilerError error in result.Errors)
                {
                    Console.Error.WriteLine(error.ErrorText);
                }

                throw new Exception($"{result.Errors.Count} problem(s) while generating assembly");
            }

            Console.WriteLine($"Compiled to {result.CompiledAssembly.Location}");

            //var result = MapperGenerator.GenerateCode(ontologyFilePath, namespaceName);

            //Console.WriteLine(result);
        }
    }
}
