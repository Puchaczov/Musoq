using System.Collections.Generic;
using Musoq.Evaluator.Optimization;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

public class TransformTree(BuildChain successor, ILoggerResolver loggerResolver) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        items.SchemaProvider = new TransitionSchemaProvider(items.SchemaProvider);

        var queryTree = items.RawQueryTree;

        var extractColumnsVisitor = new ExtractRawColumnsVisitor();
        var extractRawColumnsTraverseVisitor = new ExtractRawColumnsTraverseVisitor(extractColumnsVisitor);

        queryTree.Accept(extractRawColumnsTraverseVisitor);

        var metadata = 
            items.CreateBuildMetadataAndInferTypesVisitor?.Invoke(items.SchemaProvider, extractColumnsVisitor.Columns) ?? 
            new BuildMetadataAndInferTypesVisitor(items.SchemaProvider, extractColumnsVisitor.Columns, loggerResolver.ResolveLogger<BuildMetadataAndInferTypesVisitor>());
        var metadataTraverser = new BuildMetadataAndInferTypesTraverseVisitor(metadata);

        queryTree.Accept(metadataTraverser);
        queryTree = metadata.Root;

        var rewriter = new RewriteQueryVisitor();
        var rewriteTraverser = new RewriteQueryTraverseVisitor(rewriter, new ScopeWalker(metadataTraverser.Scope));

        queryTree.Accept(rewriteTraverser);

        queryTree = rewriter.RootScript;

        // Create optimization manager for enhanced code generation  
        var optimizationManager = new OptimizationManager();

        var csharpRewriter = new ToCSharpRewriteTreeVisitor(metadata.Assemblies, metadata.SetOperatorFieldPositions, metadata.InferredColumns, items.AssemblyName, optimizationManager);
        var csharpRewriteTraverser = new ToCSharpRewriteTreeTraverseVisitor(csharpRewriter, new ScopeWalker(metadataTraverser.Scope), items.CompilationOptions);

        queryTree.Accept(csharpRewriteTraverser);

        items.UsedColumns = metadata.UsedColumns;
        items.UsedWhereNodes = RewriteWhereNodes(metadata.UsedWhereNodes);
        items.TransformedQueryTree = queryTree;
        items.Compilation = csharpRewriter.Compilation;
        items.AccessToClassPath = csharpRewriter.AccessToClassPath;
        items.PositionalEnvironmentVariables = metadata.PositionalEnvironmentVariables;

        Successor?.Build(items);
    }

    private static IReadOnlyDictionary<SchemaFromNode, WhereNode> RewriteWhereNodes(IReadOnlyDictionary<SchemaFromNode, WhereNode> whereNodes)
    {
        var result = new Dictionary<SchemaFromNode, WhereNode>();
            
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