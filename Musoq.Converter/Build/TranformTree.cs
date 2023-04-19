using System.Collections.Generic;
using Musoq.Evaluator.Parser;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build
{
    public class TransformTree : BuildChain
    {
        public TransformTree(BuildChain successor) 
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

            var rewriter = new RewriteQueryVisitor();
            var rewriteTraverser = new RewriteQueryTraverseVisitor(rewriter, new ScopeWalker(metadataInfererTraverser.Scope));

            queryTree.Accept(rewriteTraverser);

            queryTree = rewriter.RootScript;

            var csharpRewriter = new ToCSharpRewriteTreeVisitor(metadataInferer.Assemblies, metadataInferer.SetOperatorFieldPositions, metadataInferer.InferredColumns, items.AssemblyName);
            var csharpRewriteTraverser = new ToCSharpRewriteTreeTraverseVisitor(csharpRewriter, new ScopeWalker(metadataInfererTraverser.Scope));

            queryTree.Accept(csharpRewriteTraverser);

            items.UsedColumns = metadataInferer.UsedColumns;
            items.UsedWhereNodes = RewriteWhereNodes(metadataInferer.UsedWhereNodes);
            items.TransformedQueryTree = queryTree;
            items.Compilation = csharpRewriter.Compilation;
            items.AccessToClassPath = csharpRewriter.AccessToClassPath;

            Successor?.Build(items);
        }

        private static IReadOnlyDictionary<PositionalSchemaFromNode, WhereNode> RewriteWhereNodes(IReadOnlyDictionary<PositionalSchemaFromNode, WhereNode> whereNodes)
        {
            var result = new Dictionary<PositionalSchemaFromNode, WhereNode>();
            
            foreach (var whereNode in whereNodes)
            {
                var rewriter = new RewriteWhereExpressionToPassItToDataSourceVisitor(whereNode.Key);
                var rewriteTraverser = new RewriteWhereExpressionToPassItToDataSourceTraverseVisitor(rewriter);

                whereNode.Value.Accept(rewriteTraverser);

                result.Add(whereNode.Key, rewriter.WhereNode);
            }
            
            return result;
        }
    }
}