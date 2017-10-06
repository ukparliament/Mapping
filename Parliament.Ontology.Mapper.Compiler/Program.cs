using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Configuration;

namespace Parliament.Ontology.Mapper.Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            string ontologyFilePath = ConfigurationManager.AppSettings["OntologyFilePath"];
            string namespaceName = ConfigurationManager.AppSettings["NamespaceName"];
            MapperGenerator generator = new MapperGenerator();
            CodeCompileUnit unit = generator.GenerateCodeDom(ontologyFilePath, namespaceName);
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerResults result = provider.CompileAssemblyFromDom(new CompilerParameters() { OutputAssembly = $"{namespaceName}.dll" }, new CodeCompileUnit[] { unit });
            if (result.Errors.Count > 0)
            {
                foreach (CompilerError error in result.Errors)
                    Console.Error.WriteLine(error.ErrorText);
                throw new Exception($"{result.Errors.Count} problem(s) while generating assembly");
            }
            else
                Console.WriteLine($"Compiled to {result.CompiledAssembly.Location}");
            Console.ReadLine();
        }
        
    }
}
