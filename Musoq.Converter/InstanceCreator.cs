using Musoq.Evaluator;
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

            var rewriter = new RewriteTreeVisitor((TransitionSchemaProvider)schemaProvider);
            var rewriteTraverser = new RewriteTreeTraverseVisitor(rewriter);

            query.Accept(rewriteTraverser);

            query = rewriter.RootScript;

            var metadataCreator = new PreGenerationVisitor();
            var metadataTraverser = new PreGenerationTraverseVisitor(metadataCreator);

            query.Accept(metadataTraverser);

            var codeGenerator = new CodeGenerationVisitor(schemaProvider, metadataCreator.TableMetadata);
            var traverser =
                new CodeGenerationTraverseVisitor(codeGenerator, metadataCreator.AggregationMethods.AsReadOnly());

            query.Accept(traverser);

            return codeGenerator.VirtualMachine;
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