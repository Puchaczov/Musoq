using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;

namespace Musoq.Converter.Build
{
    public class TranformTree : BuildChain
    {
        public TranformTree(BuildChain successor) 
            : base(successor)
        {
        }

        public override void Build(BuildItems items)
        {
            items.SchemaProvider = new TransitionSchemaProvider(items.SchemaProvider);

            var queryTree = items.RawQueryTree;

            var metadataInferer = new BuildMetadataAndInferTypeVisitor(items.SchemaProvider);
            var metadataInfererTraverser = new BuildMetadataAndInferTypeTraverseVisitor(metadataInferer);

            queryTree.Accept(metadataInfererTraverser);

            queryTree = metadataInferer.Root;

            var rewriter = new RewriteQueryVisitor((TransitionSchemaProvider)items.SchemaProvider);
            var rewriteTraverser = new RewriteQueryTraverseVisitor(rewriter, new ScopeWalker(metadataInfererTraverser.Scope));

            queryTree.Accept(rewriteTraverser);

            queryTree = rewriter.RootScript;

            var csharpRewriter = new ToCSharpRewriteTreeVisitor(metadataInferer.Assemblies, metadataInferer.SetOperatorFieldPositions, string.Empty);
            var csharpRewriteTraverser = new ToCSharpRewriteTreeTraverseVisitor(csharpRewriter, new ScopeWalker(metadataInfererTraverser.Scope));

            queryTree.Accept(csharpRewriteTraverser);

            items.TransformedQueryTree = queryTree;
            items.Compilation = csharpRewriter.Compilation;
            items.AccessToClassPath = csharpRewriter.AccessToClassPath;

            Successor?.Build(items);
        }
    }
}