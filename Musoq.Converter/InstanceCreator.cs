using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Musoq.Evaluator;
using Musoq.Evaluator.TemporarySchemas;
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
        public static CompiledQuery Create(RootNode root, ISchemaProvider schemaProvider)
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

#if DEBUG
            string tempPath = Path.GetTempPath();
            string tempFileName = $"{Guid.NewGuid().ToString()}";
            string assemblyPath = Path.Combine(tempPath, $"{tempFileName}.dll");
            string pdbPath = Path.Combine(tempPath, $"{tempFileName}.pdb");
            string csPath = Path.Combine(tempPath, $"{tempFileName}.cs");


            var csharpRewriter =
                new ToCSharpRewriteTreeVisitor(metadataInferer.Assemblies, metadataInferer.SetOperatorFieldPositions, csPath);
            var csharpRewriteTraverser = new ToCSharpRewriteTreeTraverseVisitor(csharpRewriter,
                new ScopeWalker(metadataInfererTraverser.Scope));

            query.Accept(csharpRewriteTraverser);

            using (var stream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var result = csharpRewriter.Compilation.Emit(stream, pdbStream);

                var builder = new StringBuilder();
                if (!result.Success)
                {
                    var all = new StringBuilder();

                    foreach (var diagnostic in result.Diagnostics)
                        all.Append(diagnostic);

                    builder = new StringBuilder();
                    using (var writer = new StringWriter(builder))
                    {
                        csharpRewriter.Compilation.SyntaxTrees[0].GetRoot().WriteTo(writer);
                    }

                    using (var file = new StreamWriter(File.OpenWrite(csPath)))
                    {
                        file.Write(builder.ToString());
                    }

                    throw new NotSupportedException(all.ToString());
                }
                
                var assembly = Assembly.Load(stream.ToArray(), pdbStream.ToArray());

                var type = assembly.GetType(csharpRewriter.AccessToClassPath);

                var runnable = (IRunnable)Activator.CreateInstance(type);
                runnable.Provider = schemaProvider;

                if (!Debugger.IsAttached) return new CompiledQuery(runnable);

                builder = new StringBuilder();
                using (var writer = new StringWriter(builder))
                {
                    csharpRewriter.Compilation.SyntaxTrees[0].GetRoot().WriteTo(writer);
                }

                using (var file = new StreamWriter(File.OpenWrite(csPath)))
                {
                    file.Write(builder.ToString());
                }

                using (var file = new BinaryWriter(File.OpenWrite(assemblyPath)))
                {
                    file.Write(stream.ToArray());
                }

                runnable = new RunnableDebugDecorator(runnable, csPath, assemblyPath);

                return new CompiledQuery(runnable);
            }
#else
            var csharpRewriter =
                new ToCSharpRewriteTreeVisitor(metadataInferer.Assemblies, metadataInferer.SetOperatorFieldPositions, string.Empty);
            var csharpRewriteTraverser = new ToCSharpRewriteTreeTraverseVisitor(csharpRewriter,
                new ScopeWalker(metadataInfererTraverser.Scope));

            query.Accept(csharpRewriteTraverser);

            using (var stream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                EmitResult result = csharpRewriter.Compilation.Emit(stream);

                if (!result.Success)
                {
                    var all = new StringBuilder();

                    foreach (var diagnostic in result.Diagnostics)
                        all.Append(diagnostic);

                    throw new NotSupportedException(all.ToString());
                }

                var assembly = Assembly.Load(stream.ToArray(), pdbStream.ToArray());

                var type = assembly.GetType(csharpRewriter.AccessToClassPath);

                var runnable = (IRunnable)Activator.CreateInstance(type);
                runnable.Provider = schemaProvider;

                return new CompiledQuery(runnable);
            }
#endif
        }

        public static CompiledQuery Create(string script, ISchemaProvider schemaProvider)
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