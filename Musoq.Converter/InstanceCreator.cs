using System;
using System.IO;
using System.Reflection;
using System.Text;
using Musoq.Evaluator;
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

            var rewriter = new RewriteQueryVisitor((TransitionSchemaProvider) schemaProvider);
            var rewriteTraverser =
                new RewriteQueryTraverseVisitor(rewriter, new ScopeWalker(metadataInfererTraverser.Scope));

            query.Accept(rewriteTraverser);

            query = rewriter.RootScript;

            var csharpRewriter =
                new ToCSharpRewriteTreeVisitor(metadataInferer.Assemblies, metadataInferer.SetOperatorFieldPositions);
            var csharpRewriteTraverser = new ToCSharpRewriteTreeTraverseVisitor(csharpRewriter,
                new ScopeWalker(metadataInfererTraverser.Scope));

            query.Accept(csharpRewriteTraverser);

#if DEBUG
            using (var stream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var builder = new StringBuilder();
                using (var writer = new StringWriter(builder))
                {
                    csharpRewriter.Compilation.SyntaxTrees[0].GetRoot().WriteTo(writer);
                }

                var testPath = Path.Combine("E:\\Temp",
                    $"{Guid.NewGuid().ToString()}.cs");

                using (var file = new StreamWriter(File.OpenWrite(testPath)))
                {
                    file.Write(builder.ToString());
                }

                var result = csharpRewriter.Compilation.Emit(stream, pdbStream);

                if (!result.Success)
                {
                    var all = new StringBuilder();

                    foreach (var diagnostic in result.Diagnostics)
                        all.Append(diagnostic);

                    throw new NotSupportedException(all.ToString());
                }

                var assembly = Assembly.Load(stream.ToArray(), pdbStream.ToArray());

                var type = assembly.GetType("Query.Compiled.CompiledQuery");

                var runnable = (IRunnable) Activator.CreateInstance(type);
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

                    var runnable = (IRunnable)Activator.CreateInstance(type);
                    runnable.Provider = schemaProvider;
                    return runnable;
                }
            }
#endif
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