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
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Converter
{
    public static class InstanceCreator
    {
        public static IRunnable Create(RootNode root, ISchemaProvider schemaProvider)
        {
            schemaProvider = new TransitionSchemaProvider(schemaProvider);

            var query = root;

            var metadataInferer = new BuildMetadataAndInferTypeVisitor(schemaProvider);
            var metadataInfererTraverser = new BuildMetadataAndInferTypeTraverseVisitor(metadataInferer);

            query.Accept(metadataInfererTraverser);

            query = metadataInferer.Root;

            var rewriter = new RewriteQueryVisitor((TransitionSchemaProvider)schemaProvider, metadataInferer.RefreshMethods);
            var rewriteTraverser = new BuildMetadataAndInferTypeTraverseVisitor(rewriter);

            query.Accept(rewriteTraverser);

            query = rewriter.RootScript;

            var csharpRewriter = new ToCSharpRewriteTreeVisitor(metadataInferer.Assemblies);
            var csharpRewriteTraverser = new ToCSharpRewriteTreeTraverseVisitor(csharpRewriter, schemaProvider, new ScopeWalker(metadataInfererTraverser.Scope));

            query.Accept(csharpRewriteTraverser);

#if DEBUG
            using (var stream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                EmitResult result = csharpRewriter.Compilation.Emit(stream, pdbStream);

                if (!result.Success) throw new NotSupportedException();

                var assembly = Assembly.Load(stream.ToArray(), pdbStream.ToArray());

                var type = assembly.GetType("Query.Compiled.CompiledQuery");

                var runnable = (IRunnable)Activator.CreateInstance(type);
                runnable.Provider = schemaProvider;
                return runnable;
            }
#else
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
#endif

            throw new NotSupportedException();
        }


        public static IRunnable Create(string script, ISchemaProvider schemaProvider)
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