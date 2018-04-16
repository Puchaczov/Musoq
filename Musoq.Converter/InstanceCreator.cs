using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Converter
{
    public static class InstanceCreator
    {
        public static VirtualMachine Create(RootNode root, ISchemaProvider schemaProvider)
        {
            schemaProvider = new TransitionSchemaProvider(schemaProvider);

            var query = root;

            var rewriter = new RewriteTreeVisitor((TransitionSchemaProvider)schemaProvider);
            var rewriteTraverser = new RewriteTreeTraverseVisitor(rewriter);

            query.Accept(rewriteTraverser);

            query = rewriter.RootScript;

            var metadataCreator = new PreGenerationVisitor();
            var metadataTraverser = new PreGenerationTraverseVisitor(metadataCreator);

            query.Accept(metadataTraverser);

            var codeGenerator = new CodeGenerationVisitor(schemaProvider, metadataCreator.TableMetadata);
            var traverser =
                new CodeGenerationTraverseVisitor(codeGenerator);

            query.Accept(traverser);

            return codeGenerator.VirtualMachine;
        }

        public static Func<Table> CreateCompiled(RootNode root, ISchemaProvider schemaProvider)
        {
            schemaProvider = new TransitionSchemaProvider(schemaProvider);

            var query = root;

            var rewriter = new RewriteTreeVisitor((TransitionSchemaProvider)schemaProvider);
            var rewriteTraverser = new RewriteTreeTraverseVisitor(rewriter);

            query.Accept(rewriteTraverser);

            query = rewriter.RootScript;

            var metadataCreator = new PreGenerationVisitor();
            var metadataTraverser = new PreGenerationTraverseVisitor(metadataCreator);

            query.Accept(metadataTraverser);

            var codeGenerator = new RuntimeGeneratorVisitor();
            var traverser =
                new CodeGenerationTraverseVisitor(codeGenerator);

            query.Accept(traverser);

            var syntaxTree = CSharpSyntaxTree.ParseText(codeGenerator.Script);

            var compilation = CSharpCompilation.Create(
                "assemblyName",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                if (!emitResult.Success)
                {
                    throw new NotSupportedException();
                }

                Assembly asm = Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());

                var type = asm.GetType("Musoq.CodeGenerated.TranslatedQuery");
                object obj = Activator.CreateInstance(type);
                var func = type.InvokeMember("ComputeQuery",
                    BindingFlags.Default | BindingFlags.InvokeMethod,
                    null,
                    obj,
                    new object[0]);

                return ((Func<Table>)func);
            }

            throw new NotImplementedException();
        }


        public static VirtualMachine Create(string script, ISchemaProvider schemaProvider)
        {
            return Create(CreateTree(script), schemaProvider);
        }

        public static RootNode CreateTree(string script)
        {
            var lexer = new Lexer(script, true);
            var parser = new FqlParser(lexer);

            return parser.ComposeAll();
        }
    }
}