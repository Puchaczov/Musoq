using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Formatting;
using Musoq.Evaluator;
using Musoq.Evaluator.Instructions;
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
        public static IVirtualMachine Create(RootNode root, ISchemaProvider schemaProvider)
        {
            schemaProvider = new TransitionSchemaProvider(schemaProvider);

            var query = root;

            var metadataInferer = new BuildMetadataAndInferTypeVisitor(schemaProvider);
            var metadataInfererTraverser = new RewriteTreeTraverseVisitor(metadataInferer);

            query.Accept(metadataInfererTraverser);

            query = metadataInferer.Root;

            var columnsCollector = new CollectColumnsBySchemasVisitor(schemaProvider);
            var columnsCollectorTraverser = new CollectColumnsBySchemasTraverseVisitor(columnsCollector);

            query.Accept(columnsCollectorTraverser);

            var csharpRewriter = new ToCSharpRewriteTreeVisitor(metadataInferer.Assemblies);
            var csharpRewriteTraverser = new RewriteTreeTraverseVisitor(csharpRewriter);

            query.Accept(csharpRewriteTraverser);

            using (var stream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                EmitResult result = csharpRewriter.Compilation.Emit(stream);

                if (result.Success)
                {
                    var assembly = Assembly.Load(stream.ToArray(), pdbStream.ToArray());

                    var type = assembly.GetType("Query.Compiled.CompiledQuery");
                    var method = type.GetMethod("RunQuery");

                    var obj = Activator.CreateInstance(type);
                    return new CompiledMachine(obj, schemaProvider, method);
                }
            }

            throw new NotSupportedException();
        }


        public static IVirtualMachine Create(string script, ISchemaProvider schemaProvider)
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