using Parliament.Serialization;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Parliament.Ontology.ModelCodeDom
{
    public class RubyCodeProvider : CodeDomProvider
    {
        private IndentedTextWriter output;
        private CodeTypeDeclaration[] codeTypes;
        private List<string> namespaceCodeTypes = new List<string>();

        public override string FileExtension
        {
            get
            {
                return "rb";
            }
        }


        [Obsolete]
        public override ICodeCompiler CreateCompiler()
        {
            throw new NotImplementedException();
        }

        [Obsolete]
        public override ICodeGenerator CreateGenerator()
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeFromCompileUnit(CodeCompileUnit compileUnit, TextWriter writer, CodeGeneratorOptions options)
        {
            options = options ?? new CodeGeneratorOptions();
            output = new IndentedTextWriter(writer, options.IndentString);
            generateRdfNamespace(writer, options);
            foreach (CodeNamespace codeNamespace in compileUnit.Namespaces)
                GenerateCodeFromNamespace(codeNamespace, output.InnerWriter, options);
            output = null;
        }

        public override void GenerateCodeFromNamespace(CodeNamespace codeNamespace, TextWriter writer, CodeGeneratorOptions options)
        {
            generateNamespaceTop(codeNamespace.Name, writer);
            codeTypes = codeNamespace.Types.OfType<CodeTypeDeclaration>()
                .Where(cn => (cn.IsInterface))
                .ToArray();
            namespaceCodeTypes = codeTypes.Select(ct => ct.Name).ToList();
            output.Indent++;
            while (namespaceCodeTypes.Any())
            {
                string codeTypeName = namespaceCodeTypes.FirstOrDefault();
                CodeTypeDeclaration codeType = codeTypes.SingleOrDefault(ct => ct.Name == codeTypeName);
                GenerateCodeFromType(codeType, writer, options);
            }
            output.Indent--;
            generateNamespaceBottom(codeNamespace.Name, writer);
        }

        public override void GenerateCodeFromType(CodeTypeDeclaration codeType, TextWriter writer, CodeGeneratorOptions options)
        {
            foreach (CodeTypeReference parentClass in codeType.BaseTypes)
            {
                if ((parentClass.BaseType != typeof(BaseResource).FullName) &&
                    (namespaceCodeTypes.Contains(parentClass.BaseType) == true))
                {
                    CodeTypeDeclaration codeTypeDeclaration = codeTypes.SingleOrDefault(ct => ct.Name == parentClass.BaseType);
                    GenerateCodeFromType(codeTypeDeclaration, writer, options);
                }
            }
            namespaceCodeTypes.Remove(codeType.Name);
            output.WriteLine($"module {codeType.Name}");
            output.Indent++;
            foreach (CodeTypeReference parentClass in codeType.BaseTypes)
                output.WriteLine($"include {parentClass.BaseType.Replace(".", "::")}");
            if (codeType.Members.Count > 0)
            {
                output.WriteLine();
                output.Write("attr_accessor ");
                for (int i = 0; i < codeType.Members.Count; i++)
                {
                    if (i == 1)
                        output.Indent++;
                    output.Write($":{codeType.Members[i].Name}");
                    if (i < codeType.Members.Count - 1)
                        output.WriteLine(",");
                }
                if (codeType.Members.Count > 1)
                    output.Indent--;
                output.WriteLine();
            }
            output.Indent--;
            output.WriteLine("end");
            output.WriteLine();
            output.WriteLine($"class {codeType.Name.Remove(0,1)}");
            output.Indent++;
            output.WriteLine($"include {codeType.Name}");
            output.Indent--;
            output.WriteLine("end");
            output.WriteLine();
        }

        public override void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options)
        {
        }

        private void generateRdfNamespace(TextWriter writer, CodeGeneratorOptions options)
        {
            Type rdfType = typeof(BaseResource);
            generateNamespaceTop(rdfType.Namespace, writer);
            CodeTypeDeclaration codeTypeDeclaration = new CodeTypeDeclaration(rdfType.Name);
            foreach (PropertyInfo propertyInfo in rdfType.GetProperties())
                codeTypeDeclaration.Members.Add(new CodeMemberProperty()
                {
                    Name = propertyInfo.Name,
                    Type = new CodeTypeReference(propertyInfo.DeclaringType)
                });
            output.Indent++;
            GenerateCodeFromType(codeTypeDeclaration, writer, options);
            output.Indent--;
            generateNamespaceBottom(rdfType.Namespace, writer);
        }

        private void generateNamespaceTop(string namespaceName, TextWriter writer)
        {
            string[] scopes = namespaceName.Split('.');
            for (int i = 0; i < scopes.Length; i++)
            {
                output.Indent = i;
                output.WriteLine($"module {scopes[i]}");
            }
        }

        private void generateNamespaceBottom(string namespaceName, TextWriter writer)
        {
            for (int i = namespaceName.Split('.').Length - 1; i >= 0; i--)
            {
                output.Indent = i;
                output.WriteLine("end");
            }
        }

    }
}
